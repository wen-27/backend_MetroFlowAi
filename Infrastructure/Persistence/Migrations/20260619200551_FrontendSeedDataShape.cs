using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FrontendSeedDataShape : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Station",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OccupancyCurrent",
                table: "Station",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OccupancyPrediction20Min",
                table: "Station",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Recommendation",
                table: "Station",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ActiveBuses",
                table: "Route",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AvgTimeMinutes",
                table: "Route",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DelayMinutes",
                table: "Route",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Destination",
                table: "Route",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FrontendStatus",
                table: "Route",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OccupancyLevel",
                table: "Route",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Route",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PathCoordinatesJson",
                table: "Route",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExternalCode",
                table: "OperationalRecommendation",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FrontendType",
                table: "OperationalRecommendation",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Impact",
                table: "OperationalRecommendation",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetCode",
                table: "OperationalRecommendation",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ActiveDurationMinutes",
                table: "Incident",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AffectedRoute",
                table: "Incident",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExternalCode",
                table: "Incident",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FrontendStatus",
                table: "Incident",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Incident",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OfficerInCharge",
                table: "Incident",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "Bus",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EtaMinutes",
                table: "Bus",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NextStation",
                table: "Bus",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExternalCode",
                table: "Alert",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FrontendStatus",
                table: "Alert",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Recommendation",
                table: "Alert",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Target",
                table: "Alert",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Station");

            migrationBuilder.DropColumn(
                name: "OccupancyCurrent",
                table: "Station");

            migrationBuilder.DropColumn(
                name: "OccupancyPrediction20Min",
                table: "Station");

            migrationBuilder.DropColumn(
                name: "Recommendation",
                table: "Station");

            migrationBuilder.DropColumn(
                name: "ActiveBuses",
                table: "Route");

            migrationBuilder.DropColumn(
                name: "AvgTimeMinutes",
                table: "Route");

            migrationBuilder.DropColumn(
                name: "DelayMinutes",
                table: "Route");

            migrationBuilder.DropColumn(
                name: "Destination",
                table: "Route");

            migrationBuilder.DropColumn(
                name: "FrontendStatus",
                table: "Route");

            migrationBuilder.DropColumn(
                name: "OccupancyLevel",
                table: "Route");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Route");

            migrationBuilder.DropColumn(
                name: "PathCoordinatesJson",
                table: "Route");

            migrationBuilder.DropColumn(
                name: "ExternalCode",
                table: "OperationalRecommendation");

            migrationBuilder.DropColumn(
                name: "FrontendType",
                table: "OperationalRecommendation");

            migrationBuilder.DropColumn(
                name: "Impact",
                table: "OperationalRecommendation");

            migrationBuilder.DropColumn(
                name: "TargetCode",
                table: "OperationalRecommendation");

            migrationBuilder.DropColumn(
                name: "ActiveDurationMinutes",
                table: "Incident");

            migrationBuilder.DropColumn(
                name: "AffectedRoute",
                table: "Incident");

            migrationBuilder.DropColumn(
                name: "ExternalCode",
                table: "Incident");

            migrationBuilder.DropColumn(
                name: "FrontendStatus",
                table: "Incident");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Incident");

            migrationBuilder.DropColumn(
                name: "OfficerInCharge",
                table: "Incident");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "Bus");

            migrationBuilder.DropColumn(
                name: "EtaMinutes",
                table: "Bus");

            migrationBuilder.DropColumn(
                name: "NextStation",
                table: "Bus");

            migrationBuilder.DropColumn(
                name: "ExternalCode",
                table: "Alert");

            migrationBuilder.DropColumn(
                name: "FrontendStatus",
                table: "Alert");

            migrationBuilder.DropColumn(
                name: "Recommendation",
                table: "Alert");

            migrationBuilder.DropColumn(
                name: "Target",
                table: "Alert");
        }
    }
}
