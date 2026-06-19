namespace Domain.Stations.ValueObjects;

public readonly record struct StationId(Guid Value);
public readonly record struct StationCode(string Value);
public readonly record struct StationName(string Value);
public readonly record struct GeoPoint(decimal Latitude, decimal Longitude);

