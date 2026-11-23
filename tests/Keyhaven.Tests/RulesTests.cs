using Keyhaven.Web.Models;
using Keyhaven.Web.Services;

namespace Keyhaven.Tests;

public class MaintenanceTriageTests
{
    [Fact]
    public void WaterIngress_IsTreatedAsAnEmergencyPlumbingJob()
    {
        var result = MaintenanceTriageService.Triage(
            "Leak in hallway",
            "Water is coming through the ceiling from the flat above.",
            MaintenanceCategory.Other);

        Assert.Equal(Urgency.Emergency, result.Urgency);
        Assert.Equal(MaintenanceCategory.Plumbing, result.Category);
        Assert.Contains("Plumbing", result.SuggestedTrade);
        Assert.Contains("ingress", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NoHotWater_IsUrgent()
    {
        var result = MaintenanceTriageService.Triage(
            "No hot water",
            "The boiler is firing but there is no hot water at the taps.",
            MaintenanceCategory.Heating);

        Assert.Equal(Urgency.Urgent, result.Urgency);
        Assert.Equal(MaintenanceCategory.Heating, result.Category);
    }

    [Fact]
    public void StiffLock_IsRoutineAndInferredAsLocks()
    {
        var result = MaintenanceTriageService.Triage(
            "Front door lock very stiff",
            "The lock still works but needs a firm push and turn.",
            MaintenanceCategory.Other);

        Assert.Equal(Urgency.Routine, result.Urgency);
        Assert.Equal(MaintenanceCategory.Locks, result.Category);
    }

    [Fact]
    public void CosmeticWork_HasNoEmergencySignals()
    {
        var result = MaintenanceTriageService.Triage(
            "Hallway redecoration",
            "Fill and repaint the hallway after check-out.",
            MaintenanceCategory.Other);

        Assert.Equal(Urgency.Routine, result.Urgency);
        Assert.Contains("Cosmetic or turnaround", result.Reason);
    }
}

public class ComplianceStateTests
{
    private static readonly DateOnly Today = new(2026, 1, 1);

    [Theory]
    [InlineData(-1, ComplianceState.Expired)]
    [InlineData(0, ComplianceState.ExpiringSoon)]
    [InlineData(30, ComplianceState.ExpiringSoon)]
    [InlineData(31, ComplianceState.Valid)]
    [InlineData(365, ComplianceState.Valid)]
    public void ExpiryState_IsDrivenByThirtyDayWindow(int daysFromToday, ComplianceState expected)
    {
        var state = ComplianceService.StateFor(Today.AddDays(daysFromToday), Today);
        Assert.Equal(expected, state);
    }

    [Fact]
    public void MissingCertificate_IsMissing()
    {
        Assert.Equal(ComplianceState.Missing, ComplianceService.StateFor(null, Today));
    }

    [Fact]
    public void Hmo_RequiresHmoLicenceAsWellAsStandardChecks()
    {
        var property = new Property { Type = PropertyType.Hmo };
        var required = ComplianceService.RequiredFor(property);

        Assert.Contains(ComplianceType.GasSafety, required);
        Assert.Contains(ComplianceType.Epc, required);
        Assert.Contains(ComplianceType.Eicr, required);
        Assert.Contains(ComplianceType.HmoLicence, required);
    }
}

public class ArrearsDraftTests
{
    [Fact]
    public void ChaserDraft_QuotesTheLedgerFacts()
    {
        var tenancy = new Tenancy
        {
            RentAmount = 1850m,
            Property = new Property { AddressLine1 = "48 Richmond Road", City = "Bristol" },
            Tenant = new Tenant { FullName = "Daniel Okafor" }
        };

        var draft = ArrearsService.BuildChaserDraft(
            tenancy,
            3200m,
            new DateOnly(2026, 10, 1),
            "Daniel Okafor");

        Assert.Contains("£3,200.00", draft);
        Assert.Contains("1 October 2026", draft);
        Assert.Contains("48 Richmond Road, Bristol", draft);
        Assert.Contains("Daniel Okafor", draft);
        Assert.Contains("payment arrangement", draft);
    }
}
