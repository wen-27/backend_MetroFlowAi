using Application.Routing.Models;
using FluentValidation;

namespace Application.Routing.UseCase;

public sealed class RoutePlanRequestValidator : AbstractValidator<RoutePlanRequest>
{
    public RoutePlanRequestValidator()
    {
        RuleFor(x => x.OriginStationId).NotEmpty();
        RuleFor(x => x.DestinationStationId).NotEmpty().NotEqual(x => x.OriginStationId);
    }
}

