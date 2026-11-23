using System.ComponentModel.DataAnnotations;

namespace Keyhaven.Web.Models;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Landlord = "Landlord";
    public const string Tenant = "Tenant";
    public const string Contractor = "Contractor";

    public static readonly string[] Staff = [Admin, Manager];
}

public enum OwnerStatus
{
    [Display(Name = "Prospect")] Prospect,
    [Display(Name = "Onboarding")] Onboarding,
    [Display(Name = "Active")] Active,
    [Display(Name = "Archived")] Archived
}

public enum PropertyType
{
    [Display(Name = "Flat")] Flat,
    [Display(Name = "Apartment")] Apartment,
    [Display(Name = "Terraced house")] TerracedHouse,
    [Display(Name = "Semi-detached house")] SemiDetached,
    [Display(Name = "Detached house")] Detached,
    [Display(Name = "HMO")] Hmo,
    [Display(Name = "Bungalow")] Bungalow
}

public enum PropertyStatus
{
    [Display(Name = "Available")] Available,
    [Display(Name = "Under offer")] UnderOffer,
    [Display(Name = "Let")] Let,
    [Display(Name = "Maintenance")] Maintenance,
    [Display(Name = "Archived")] Archived
}

public enum ReferencingStatus
{
    [Display(Name = "Not started")] NotStarted,
    [Display(Name = "In progress")] InProgress,
    [Display(Name = "Passed")] Passed,
    [Display(Name = "Failed")] Failed
}

public enum TenancyStatus
{
    [Display(Name = "Pending")] Pending,
    [Display(Name = "Active")] Active,
    [Display(Name = "Ending")] Ending,
    [Display(Name = "Ended")] Ended
}

public enum RentReconciliation
{
    [Display(Name = "Unreconciled")] Unreconciled,
    [Display(Name = "Matched")] Matched,
    [Display(Name = "Shortfall")] Shortfall,
    [Display(Name = "Overpaid")] Overpaid
}

public enum MaintenanceCategory
{
    [Display(Name = "Plumbing")] Plumbing,
    [Display(Name = "Heating")] Heating,
    [Display(Name = "Electrical")] Electrical,
    [Display(Name = "Appliance")] Appliance,
    [Display(Name = "Damp & mould")] Damp,
    [Display(Name = "Structural")] Structural,
    [Display(Name = "Locks & security")] Locks,
    [Display(Name = "Other")] Other
}

public enum Urgency
{
    [Display(Name = "Emergency")] Emergency,
    [Display(Name = "Urgent")] Urgent,
    [Display(Name = "Routine")] Routine
}

public enum MaintenanceStatus
{
    [Display(Name = "New")] New,
    [Display(Name = "Triaged")] Triaged,
    [Display(Name = "Quoted")] Quoted,
    [Display(Name = "Awaiting approval")] AwaitingApproval,
    [Display(Name = "Assigned")] Assigned,
    [Display(Name = "In progress")] InProgress,
    [Display(Name = "Completed")] Completed,
    [Display(Name = "Closed")] Closed
}

public enum ComplianceType
{
    [Display(Name = "Gas Safety")] GasSafety,
    [Display(Name = "EPC")] Epc,
    [Display(Name = "EICR")] Eicr,
    [Display(Name = "HMO licence")] HmoLicence,
    [Display(Name = "Legionella")] Legionella
}

public enum ComplianceState
{
    [Display(Name = "Valid")] Valid,
    [Display(Name = "Expiring soon")] ExpiringSoon,
    [Display(Name = "Expired")] Expired,
    [Display(Name = "Missing")] Missing
}

public enum RenewalStatus
{
    [Display(Name = "Not scheduled")] None,
    [Display(Name = "Contractor assigned")] Assigned,
    [Display(Name = "Booked")] Booked,
    [Display(Name = "Completed")] Completed
}

public enum ViewingStatus
{
    [Display(Name = "Requested")] Requested,
    [Display(Name = "Confirmed")] Confirmed,
    [Display(Name = "Completed")] Completed,
    [Display(Name = "No show")] NoShow,
    [Display(Name = "Cancelled")] Cancelled
}

public enum PrescreenStatus
{
    [Display(Name = "Not started")] NotStarted,
    [Display(Name = "Invited")] Invited,
    [Display(Name = "Passed")] Passed,
    [Display(Name = "Failed")] Failed
}

public enum EnquirySource
{
    [Display(Name = "Agency website")] Website,
    [Display(Name = "Rightmove")] Rightmove,
    [Display(Name = "Zoopla")] Zoopla,
    [Display(Name = "Find your Home")] FindPortal,
    [Display(Name = "Phone")] Phone,
    [Display(Name = "Walk-in")] WalkIn
}

public enum EnquiryStatus
{
    [Display(Name = "New")] New,
    [Display(Name = "Replied")] Replied,
    [Display(Name = "Converted")] Converted,
    [Display(Name = "Closed")] Closed
}

public enum KeyType
{
    [Display(Name = "House key")] HouseKey,
    [Display(Name = "ID fob")] Fob,
    [Display(Name = "Master key")] MasterKey,
    [Display(Name = "Post box key")] PostBox
}

public enum InboxCategory
{
    [Display(Name = "Enquiry")] Enquiry,
    [Display(Name = "Repair")] Repair,
    [Display(Name = "Access")] Access,
    [Display(Name = "Statement")] Statement,
    [Display(Name = "Rent")] Rent,
    [Display(Name = "Viewing")] Viewing,
    [Display(Name = "Other")] Other
}

public enum InboxStatus
{
    [Display(Name = "New")] New,
    [Display(Name = "Draft ready")] DraftReady,
    [Display(Name = "Auto-replied")] AutoReplied,
    [Display(Name = "Needs a person")] NeedsHuman,
    [Display(Name = "Replied")] Replied
}

public enum AutomationMode
{
    [Display(Name = "Pending")] Pending,
    [Display(Name = "Auto-sent")] AutoSent,
    [Display(Name = "Drafted for review")] Drafted,
    [Display(Name = "Escalated")] Escalated
}

public enum ContactType
{
    [Display(Name = "Owner")] Owner,
    [Display(Name = "Tenant")] Tenant,
    [Display(Name = "Contractor")] Contractor,
    [Display(Name = "Applicant")] Applicant,
    [Display(Name = "Other")] Other
}

public enum ContactChannel
{
    [Display(Name = "Email")] Email,
    [Display(Name = "Phone")] Phone,
    [Display(Name = "Note")] Note,
    [Display(Name = "Meeting")] Meeting
}
