using Application.Assistant.Abstractions;
using Application.Assistant.Models;
using Application.Common;
using Application.Routing.Abstractions;
using Application.Routing.Models;
using Domain.Assistant.Entities;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Assistant;

public sealed class AssistantService(IMetroFlowDbContext db, IRoutePlannerService planner) : IAssistantService
{
    public async Task<AssistantResponse> StartAsync(CancellationToken cancellationToken = default)
    {
        var session = new ChatSession { CurrentStep = "MainMenu" };
        db.ChatSessions.Add(session);
        db.ChatMessages.Add(new ChatMessage { ChatSessionId = session.Id, Sender = ChatSender.System, Message = MainMenuMessage });
        await db.SaveChangesAsync(cancellationToken);
        return new AssistantResponse(session.SessionKey, MainMenuMessage, session.CurrentStep, MainMenuOptions());
    }

    public async Task<AssistantResponse> StepAsync(AssistantStepRequest request, CancellationToken cancellationToken = default)
    {
        var session = await db.ChatSessions.FirstOrDefaultAsync(x => x.SessionKey == request.SessionId, cancellationToken)
            ?? throw new InvalidOperationException("Sesion no encontrada.");
        session.LastOptionSelected = request.SelectedOption;
        session.UpdatedAt = DateTime.UtcNow;
        db.ChatMessages.Add(new ChatMessage { ChatSessionId = session.Id, Sender = ChatSender.User, SelectedOption = request.SelectedOption, Message = request.SelectedOption.ToString() });

        var response = session.CurrentStep switch
        {
            "MainMenu" => await HandleMainMenu(session, request.SelectedOption, cancellationToken),
            "ChooseOrigin" => await ChooseOrigin(session, request.SelectedOption, cancellationToken),
            "ChooseDestination" => await ChooseDestination(session, request.SelectedOption, cancellationToken),
            "ChooseArrivalStation" => await ArrivalStation(session, request.SelectedOption, cancellationToken),
            "ChooseOccupancyRoute" => await OccupancyRoute(session, request.SelectedOption, cancellationToken),
            _ => BackToMenu(session)
        };

        db.ChatMessages.Add(new ChatMessage { ChatSessionId = session.Id, Sender = ChatSender.System, Message = response.Message });
        await db.SaveChangesAsync(cancellationToken);
        return response;
    }

    private async Task<AssistantResponse> HandleMainMenu(ChatSession session, int option, CancellationToken ct) => option switch
    {
        1 => await StationPicker(session, "ChooseOrigin", "Elige la estacion de origen:", ct),
        2 => await StationPicker(session, "ChooseArrivalStation", "Elige una estacion para ver llegadas:", ct),
        3 => await Alerts(session, ct),
        4 => await RoutePicker(session, "ChooseOccupancyRoute", "Elige una ruta para ver ocupacion:", ct),
        _ => BackToMenu(session)
    };

    private async Task<AssistantResponse> ChooseOrigin(ChatSession session, int option, CancellationToken ct)
    {
        var stations = await db.Stations.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
        if (option < 1 || option > stations.Count) return await StationPicker(session, "ChooseOrigin", "Opcion invalida. Elige origen:", ct);
        session.OriginStationId = stations[option - 1].Id;
        return await StationPicker(session, "ChooseDestination", "Elige la estacion destino:", ct);
    }

    private async Task<AssistantResponse> ChooseDestination(ChatSession session, int option, CancellationToken ct)
    {
        var stations = await db.Stations.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
        if (option < 1 || option > stations.Count) return await StationPicker(session, "ChooseDestination", "Opcion invalida. Elige destino:", ct);
        session.DestinationStationId = stations[option - 1].Id;
        session.CurrentStep = "MainMenu";
        var plan = await planner.PlanAsync(new RoutePlanRequest(session.OriginStationId!.Value, session.DestinationStationId.Value), ct);
        return new AssistantResponse(session.SessionKey, $"Ruta recomendada: {plan.RecommendedRoute}. Tiempo estimado: {plan.EstimatedMinutes} min. {plan.Summary}", session.CurrentStep, MainMenuOptions(), plan);
    }

    private async Task<AssistantResponse> ArrivalStation(ChatSession session, int option, CancellationToken ct)
    {
        var stations = await db.Stations.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
        if (option < 1 || option > stations.Count) return await StationPicker(session, "ChooseArrivalStation", "Opcion invalida. Elige estacion:", ct);
        var station = stations[option - 1];
        var arrivals = await db.ArrivalPredictions.AsNoTracking()
            .Where(x => x.StationId == station.Id)
            .Join(db.Routes, p => p.RouteId, r => r.Id, (p, r) => new { route = r.Code, minutes = p.EstimatedArrivalMinutes, occupancy = p.OccupancyLevel.ToString() })
            .OrderBy(x => x.minutes)
            .Take(5)
            .ToListAsync(ct);
        session.CurrentStep = "MainMenu";
        return new AssistantResponse(session.SessionKey, $"Proximas llegadas para {station.Name}.", session.CurrentStep, MainMenuOptions(), arrivals);
    }

    private async Task<AssistantResponse> Alerts(ChatSession session, CancellationToken ct)
    {
        var alerts = await db.Alerts.AsNoTracking().Where(x => x.IsActive).Select(x => x.Title).ToListAsync(ct);
        session.CurrentStep = "MainMenu";
        return new AssistantResponse(session.SessionKey, "Alertas activas.", session.CurrentStep, MainMenuOptions(), alerts);
    }

    private async Task<AssistantResponse> OccupancyRoute(ChatSession session, int option, CancellationToken ct)
    {
        var routes = await db.Routes.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
        if (option < 1 || option > routes.Count) return await RoutePicker(session, "ChooseOccupancyRoute", "Opcion invalida. Elige ruta:", ct);
        var route = routes[option - 1];
        var buses = await db.Buses.AsNoTracking().Where(x => x.AssignedRouteId == route.Id).ToListAsync(ct);
        session.CurrentStep = "MainMenu";
        return new AssistantResponse(session.SessionKey, $"Ocupacion de {route.Code}: {Occupancy(buses.Select(x => x.OccupancyLevel))}.", session.CurrentStep, MainMenuOptions(), buses);
    }

    private async Task<AssistantResponse> StationPicker(ChatSession session, string step, string message, CancellationToken ct)
    {
        session.CurrentStep = step;
        var stations = await db.Stations.AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        var options = stations.Select((x, index) => new AssistantOption(index + 1, x.Name)).ToList();
        return new AssistantResponse(session.SessionKey, message, step, options);
    }

    private async Task<AssistantResponse> RoutePicker(ChatSession session, string step, string message, CancellationToken ct)
    {
        session.CurrentStep = step;
        var routes = await db.Routes.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
        return new AssistantResponse(session.SessionKey, message, step, routes.Select((x, i) => new AssistantOption(i + 1, $"{x.Code} {x.Name}")).ToList());
    }

    private AssistantResponse BackToMenu(ChatSession session)
    {
        session.CurrentStep = "MainMenu";
        return new AssistantResponse(session.SessionKey, MainMenuMessage, session.CurrentStep, MainMenuOptions());
    }

    private static IReadOnlyList<AssistantOption> MainMenuOptions() =>
    [
        new(1, "Consultar mejor ruta"),
        new(2, "Ver tiempos de llegada"),
        new(3, "Consultar alertas"),
        new(4, "Ver ocupacion de rutas")
    ];

    private static OccupancyLevel Occupancy(IEnumerable<OccupancyLevel> levels) =>
        levels.DefaultIfEmpty(OccupancyLevel.Low).Max();

    private const string MainMenuMessage = "Hola, soy el asistente de MetroFlow AI. Que deseas hacer?";
}
