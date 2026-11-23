using Keyhaven.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Keyhaven.Web.Data;

public static class SeedData
{
    public const string DemoPassword = "Demo123!";

    public static async Task InitializeAsync(KeyhavenDbContext db, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        // ---------------------------------------------------------------- contractors
        var contractors = new List<Contractor>
        {
            new() { Name = "Dale Kerr", Trade = "Plumbing & heating", Email = "contractor@keyhaven.co.uk", Phone = "07700 900501", Rating = 4.8m, Notes = "Preferred plumber for Glasgow and the west. Holds agency keys." },
            new() { Name = "Heatwise Scotland", Trade = "Heating engineer", Email = "jobs@heatwise.example", Phone = "0141 496 0182", Rating = 4.6m, Notes = "Gas Safe registered. Covers central belt." },
            new() { Name = "Volt & Co Electrical", Trade = "Electrical", Email = "bookings@voltandco.example", Phone = "07700 900522", Rating = 4.7m, Notes = "NICEIC approved. EICR testing and remedial work." },
            new() { Name = "Bright & Clean", Trade = "Cleaning & turnaround", Email = "hello@brightandclean.example", Phone = "07700 900533", Rating = 4.4m, Notes = "End-of-tenancy cleans and mid-let housekeeping." },
            new() { Name = "Lockhart Locksmiths", Trade = "Locks & security", Email = "desk@lockhart.example", Phone = "07700 900544", Rating = 4.9m, Notes = "Emergency call-out, locksmith and key cutting." }
        };
        db.Contractors.AddRange(contractors);
        await db.SaveChangesAsync(ct);

        // ---------------------------------------------------------------- owners
        var owners = new List<Owner>
        {
            new()
            {
                DisplayName = "James Whitfield", CompanyName = "Whitfield Property Group", Email = "james@whitfieldproperty.example",
                Phone = "07700 900142", Address = "41 Cleveden Road, Glasgow G12 0PH", OnboardedOn = today.AddDays(-540),
                Status = OwnerStatus.Active, AgreementSigned = true, AgreementSignedOn = today.AddDays(-538),
                ManagementFeePercent = 10m, Notes = "Prefers email. Approves quotes under £200 without a call."
            },
            new()
            {
                DisplayName = "Fiona MacLeod", CompanyName = "Argyll Estates Ltd", Email = "fiona@argyllestates.example",
                Phone = "07700 900188", Address = "8 Woodlands Terrace, Glasgow G3 6DF", OnboardedOn = today.AddDays(-890),
                Status = OwnerStatus.Active, AgreementSigned = true, AgreementSignedOn = today.AddDays(-888),
                ManagementFeePercent = 12m, Notes = "Quarterly statements on the 5th. Owns a small mixed portfolio."
            },
            new()
            {
                DisplayName = "Marcus Bell", CompanyName = "Rosewood Holdings", Email = "marcus@rosewoodholdings.example",
                Phone = "07700 900211", Address = "19 Rutland Square, Edinburgh EH1 2BB", OnboardedOn = today.AddDays(-9),
                Status = OwnerStatus.Onboarding, AgreementSigned = false, ManagementFeePercent = 10m,
                Notes = "Onboarding: two Edinburgh flats joining the managed portfolio next quarter."
            },
            new()
            {
                DisplayName = "Sunita Patel", CompanyName = "S. & M. Patel", Email = "sunita.patel@example.com",
                Phone = "07700 900244", Address = "3 Adel Grange Close, Leeds LS16 8QA", OnboardedOn = today.AddDays(-1200),
                Status = OwnerStatus.Active, AgreementSigned = true, AgreementSignedOn = today.AddDays(-1195),
                ManagementFeePercent = 8m, Notes = "Long-standing client. Leeds and Manchester properties."
            }
        };
        db.Owners.AddRange(owners);
        await db.SaveChangesAsync(ct);

        // ---------------------------------------------------------------- tenants
        var tenants = new List<Tenant>
        {
            new() { FullName = "Sarah Connolly", Email = "tenant@keyhaven.co.uk", Phone = "07700 900311", ReferencingStatus = ReferencingStatus.Passed, RightToRentChecked = true, RightToRentCheckedOn = today.AddDays(-410) },
            new() { FullName = "Daniel Okafor", Email = "daniel.okafor@example.com", Phone = "07700 900322", ReferencingStatus = ReferencingStatus.Passed, RightToRentChecked = true, RightToRentCheckedOn = today.AddDays(-540) },
            new() { FullName = "Emily Fraser", Email = "emily.fraser@example.com", Phone = "07700 900333", ReferencingStatus = ReferencingStatus.Passed, RightToRentChecked = true, RightToRentCheckedOn = today.AddDays(-280) },
            new() { FullName = "Mohammed Patel", Email = "mohammed.patel@example.com", Phone = "07700 900344", ReferencingStatus = ReferencingStatus.Passed, RightToRentChecked = true, RightToRentCheckedOn = today.AddDays(-1140) },
            new() { FullName = "Chloe Bennett", Email = "chloe.bennett@example.com", Phone = "07700 900355", ReferencingStatus = ReferencingStatus.Passed, RightToRentChecked = true, RightToRentCheckedOn = today.AddDays(-430) },
            new() { FullName = "Liam O'Connor", Email = "liam.oconnor@example.com", Phone = "07700 900366", ReferencingStatus = ReferencingStatus.Passed, RightToRentChecked = true, RightToRentCheckedOn = today.AddDays(-920) }
        };
        db.Tenants.AddRange(tenants);
        await db.SaveChangesAsync(ct);

        // ---------------------------------------------------------------- users
        var hasher = new PasswordHasher<AppUser>();
        AppUser MakeUser(string name, string email, string role, int? linked = null, string? phone = null)
        {
            var user = new AppUser
            {
                FullName = name,
                Email = email,
                Role = role,
                LinkedEntityId = linked,
                Phone = phone,
                CreatedAt = now.AddMonths(-12)
            };
            user.PasswordHash = hasher.HashPassword(user, DemoPassword);
            return user;
        }

        var admin = MakeUser("Alex Morgan", "admin@keyhaven.co.uk", Roles.Admin, phone: "0141 496 0100");
        var manager = MakeUser("Priya Shah", "manager@keyhaven.co.uk", Roles.Manager, phone: "0141 496 0102");
        var landlord = MakeUser("James Whitfield", "landlord@keyhaven.co.uk", Roles.Landlord, owners[0].Id, "07700 900142");
        var tenantUser = MakeUser("Sarah Connolly", "tenant@keyhaven.co.uk", Roles.Tenant, tenants[0].Id, "07700 900311");
        var contractorUser = MakeUser("Dale Kerr", "contractor@keyhaven.co.uk", Roles.Contractor, contractors[0].Id, "07700 900501");
        db.Users.AddRange(admin, manager, landlord, tenantUser, contractorUser);
        await db.SaveChangesAsync(ct);

        owners[0].PortalUserId = landlord.Id;
        tenants[0].PortalUserId = tenantUser.Id;

        // ---------------------------------------------------------------- properties
        var properties = new List<Property>
        {
            new()
            {
                Reference = "KH-1001", AddressLine1 = "12 Clarence Drive", AddressLine2 = "Hyndland", City = "Glasgow", Postcode = "G12 9QN",
                Type = PropertyType.Flat, Bedrooms = 2, Bathrooms = 1, MonthlyRent = 1295m, Status = PropertyStatus.Let, OwnerId = owners[0].Id,
                ManagerId = manager.Id, EpcRating = "C", ListedOn = today.AddDays(-500),
                Description = "A bright first-floor flat with bay windows, a generous living room and direct access to Hyndland station.",
                MarketingHeadline = "Two-bedroom flat in the heart of Hyndland",
                MarketingSummary = "Period proportions, a south-facing living room and a quiet residential street minutes from Hyndland station.",
                ListingPublished = false, Channels = "Agency website", ImageUrl = "/img/property-glasgow.svg",
                CreatedAt = now.AddMonths(-26)
            },
            new()
            {
                Reference = "KH-1002", AddressLine1 = "48 Richmond Road", AddressLine2 = "Montpelier", City = "Bristol", Postcode = "BS6 5EW",
                Type = PropertyType.TerracedHouse, Bedrooms = 3, Bathrooms = 1, MonthlyRent = 1850m, Status = PropertyStatus.Let, OwnerId = owners[1].Id,
                ManagerId = manager.Id, EpcRating = "D", ListedOn = today.AddDays(-640),
                Description = "A three-bedroom Victorian terrace with a walled garden, close to Montpelier station and St Werburghs.",
                MarketingHeadline = "Victorian terrace with a walled garden",
                MarketingSummary = "Three bedrooms, two reception rooms and a private garden on one of Montpelier's most established streets.",
                ListingPublished = false, Channels = "Agency website", ImageUrl = "/img/property-bristol.svg",
                CreatedAt = now.AddMonths(-20)
            },
            new()
            {
                Reference = "KH-1003", AddressLine1 = "7 Bellevue Crescent", AddressLine2 = "New Town", City = "Edinburgh", Postcode = "EH3 6NA",
                Type = PropertyType.Flat, Bedrooms = 2, Bathrooms = 1, MonthlyRent = 1450m, Status = PropertyStatus.Available, OwnerId = owners[0].Id,
                ManagerId = manager.Id, EpcRating = "B", ListedOn = today.AddDays(-9),
                Description = "A second-floor Georgian flat overlooking the crescent gardens, with ornate cornicing and a recently refitted kitchen.",
                MarketingHeadline = "Georgian elegance on Bellevue Crescent",
                MarketingSummary = "Two double bedrooms, a drawing room with garden views and a quiet New Town address.",
                ListingPublished = true, Channels = "Rightmove, Find your Home, Agency website", ImageUrl = "/img/property-edinburgh.svg",
                CreatedAt = now.AddMonths(-14)
            },
            new()
            {
                Reference = "KH-1004", AddressLine1 = "22 Chorlton Road", AddressLine2 = "Hulme", City = "Manchester", Postcode = "M15 4AN",
                Type = PropertyType.Apartment, Bedrooms = 1, Bathrooms = 1, MonthlyRent = 995m, Status = PropertyStatus.Let, OwnerId = owners[3].Id,
                ManagerId = manager.Id, EpcRating = "B", ListedOn = today.AddDays(-300),
                Description = "A one-bedroom apartment in a converted mill, with a balcony and secure parking.",
                MarketingHeadline = "Light-filled mill conversion",
                MarketingSummary = "One bedroom, exposed brick and a private balcony, five minutes from Deansgate.",
                ListingPublished = false, Channels = "Agency website", ImageUrl = "/img/property-manchester.svg",
                CreatedAt = now.AddMonths(-10)
            },
            new()
            {
                Reference = "KH-1005", AddressLine1 = "114 Camberwell Road", AddressLine2 = "Camberwell", City = "London", Postcode = "SE17 1DD",
                Type = PropertyType.Flat, Bedrooms = 2, Bathrooms = 1, MonthlyRent = 2100m, Status = PropertyStatus.UnderOffer, OwnerId = owners[1].Id,
                ManagerId = manager.Id, EpcRating = "E", ListedOn = today.AddDays(-34),
                Description = "A two-bedroom flat with a shared garden, well placed for Camberwell Green and Denmark Hill.",
                MarketingHeadline = "Two bedrooms moments from Camberwell Green",
                MarketingSummary = "A well-proportioned flat with a shared garden and excellent transport links.",
                ListingPublished = true, Channels = "Rightmove, Agency website", ImageUrl = "/img/property-london.svg",
                CreatedAt = now.AddMonths(-30)
            },
            new()
            {
                Reference = "KH-1006", AddressLine1 = "3 Adel Grange Close", AddressLine2 = "Adel", City = "Leeds", Postcode = "LS16 8QA",
                Type = PropertyType.Detached, Bedrooms = 4, Bathrooms = 2, MonthlyRent = 2400m, Status = PropertyStatus.Let, OwnerId = owners[3].Id,
                ManagerId = manager.Id, EpcRating = "C", ListedOn = today.AddDays(-900),
                Description = "A four-bedroom detached family home with a large garden, garage and driveway.",
                MarketingHeadline = "Family home in Adel",
                MarketingSummary = "Four bedrooms, two bathrooms and a mature garden on a quiet cul-de-sac.",
                ListingPublished = false, Channels = "Agency website", ImageUrl = "/img/property-leeds.svg",
                CreatedAt = now.AddMonths(-38)
            },
            new()
            {
                Reference = "KH-1007", AddressLine1 = "56 Dumbarton Road", AddressLine2 = "Partick", City = "Glasgow", Postcode = "G11 6RH",
                Type = PropertyType.Flat, Bedrooms = 1, Bathrooms = 1, MonthlyRent = 895m, Status = PropertyStatus.Available, OwnerId = owners[0].Id,
                ManagerId = manager.Id, EpcRating = "C", ListedOn = today.AddDays(-16),
                Description = "A compact one-bedroom tenement flat with a renovated kitchen and shared back garden.",
                MarketingHeadline = "One-bedroom flat in Partick",
                MarketingSummary = "A tidy first-floor flat close to Partick station, with a shared garden and secure entry.",
                ListingPublished = true, Channels = "Rightmove, Find your Home, Agency website", ImageUrl = "/img/property-glasgow.svg",
                CreatedAt = now.AddMonths(-34)
            },
            new()
            {
                Reference = "KH-1008", AddressLine1 = "9 Marlborough Street", AddressLine2 = "St Paul's", City = "Bristol", Postcode = "BS1 3NX",
                Type = PropertyType.Hmo, Bedrooms = 4, Bathrooms = 2, MonthlyRent = 2600m, Status = PropertyStatus.Let, OwnerId = owners[1].Id,
                ManagerId = manager.Id, EpcRating = "C", ListedOn = today.AddDays(-700),
                Description = "A licensed four-bedroom HMO let as a whole property, with two bathrooms and a communal kitchen.",
                MarketingHeadline = "Four-bedroom HMO in St Paul's",
                MarketingSummary = "Fully licensed and let to a professional sharer group, with two bathrooms and a communal garden.",
                ListingPublished = false, Channels = "Agency website", ImageUrl = "/img/property-bristol.svg",
                CreatedAt = now.AddMonths(-22)
            }
        };
        db.Properties.AddRange(properties);
        await db.SaveChangesAsync(ct);

        // ---------------------------------------------------------------- tenancies
        var tenancies = new List<Tenancy>
        {
            new()
            {
                PropertyId = properties[0].Id, TenantId = tenants[0].Id, StartDate = monthStart.AddMonths(-26), RentAmount = 1295m,
                DepositAmount = 1295m, DepositScheme = "TDS", DepositRegistered = true, DepositRegisteredOn = monthStart.AddMonths(-26).AddDays(8), Status = TenancyStatus.Active
            },
            new()
            {
                PropertyId = properties[1].Id, TenantId = tenants[1].Id, StartDate = monthStart.AddMonths(-18), RentAmount = 1850m,
                DepositAmount = 2135m, DepositScheme = "DPS", DepositRegistered = true, DepositRegisteredOn = monthStart.AddMonths(-18).AddDays(11), Status = TenancyStatus.Active,
                Notes = "Tenant has asked about a rent payment plan. Handle with care."
            },
            new()
            {
                PropertyId = properties[3].Id, TenantId = tenants[2].Id, StartDate = monthStart.AddMonths(-9), RentAmount = 995m,
                DepositAmount = 1145m, DepositScheme = "MyDeposits", DepositRegistered = true, DepositRegisteredOn = monthStart.AddMonths(-9).AddDays(6), Status = TenancyStatus.Active
            },
            new()
            {
                PropertyId = properties[5].Id, TenantId = tenants[3].Id, StartDate = monthStart.AddMonths(-38), RentAmount = 2400m,
                DepositAmount = 2769m, DepositScheme = "TDS", DepositRegistered = true, DepositRegisteredOn = monthStart.AddMonths(-38).AddDays(9), Status = TenancyStatus.Active
            },
            new()
            {
                PropertyId = properties[7].Id, TenantId = tenants[4].Id, StartDate = monthStart.AddMonths(-14), RentAmount = 2600m,
                DepositAmount = 3000m, DepositScheme = "SafeDeposits Scotland", DepositRegistered = true, DepositRegisteredOn = monthStart.AddMonths(-14).AddDays(5), Status = TenancyStatus.Active
            },
            new()
            {
                PropertyId = properties[6].Id, TenantId = tenants[5].Id, StartDate = monthStart.AddMonths(-30), EndDate = monthStart.AddMonths(-3).AddDays(-1),
                RentAmount = 895m, DepositAmount = 995m, DepositScheme = "SafeDeposits Scotland", DepositRegistered = true,
                DepositRegisteredOn = monthStart.AddMonths(-30).AddDays(7), Status = TenancyStatus.Ended,
                Notes = "Tenant moved out at end of tenancy. Deposit returned in full."
            }
        };
        db.Tenancies.AddRange(tenancies);
        await db.SaveChangesAsync(ct);

        // ---------------------------------------------------------------- rent ledger
        void AddSchedule(Tenancy tenancy, bool currentMonthPaid, int fullyUnpaidMonths = 0, decimal partialPaidPreviousMonth = 0m)
        {
            for (var i = 5; i >= 0; i--)
            {
                var periodStart = monthStart.AddMonths(-i);
                var dueDate = periodStart;
                var charge = new RentCharge
                {
                    TenancyId = tenancy.Id,
                    PeriodStart = periodStart,
                    DueDate = dueDate,
                    Amount = tenancy.RentAmount,
                    BankReference = $"KH{tenancy.Id:D2}{periodStart:yyyyMM}"
                };

                var isCurrent = i == 0;
                var isFullyUnpaid = i > 0 && i <= fullyUnpaidMonths;
                var isPartial = i == 1 && partialPaidPreviousMonth > 0m;

                if (isPartial)
                {
                    charge.AmountPaid = partialPaidPreviousMonth;
                    charge.PaidOn = dueDate.AddDays(6);
                    charge.Reconciliation = RentReconciliation.Shortfall;
                    charge.Notes = "Partial payment received. Balance to follow.";
                }
                else if (isFullyUnpaid || (isCurrent && !currentMonthPaid))
                {
                    charge.AmountPaid = 0m;
                    charge.Reconciliation = RentReconciliation.Unreconciled;
                    if (i == 1 && partialPaidPreviousMonth == 0m && fullyUnpaidMonths > 0)
                    {
                        charge.ChaserSentOn = today.AddDays(-7);
                    }
                }
                else
                {
                    charge.AmountPaid = tenancy.RentAmount;
                    var paid = dueDate.AddDays(2);
                    charge.PaidOn = paid > today ? today : paid;
                    charge.Reconciliation = RentReconciliation.Matched;
                }

                db.RentCharges.Add(charge);
            }
        }

        AddSchedule(tenancies[0], currentMonthPaid: true);
        AddSchedule(tenancies[1], currentMonthPaid: false, fullyUnpaidMonths: 1, partialPaidPreviousMonth: 500m);
        AddSchedule(tenancies[2], currentMonthPaid: true);
        AddSchedule(tenancies[3], currentMonthPaid: false);
        AddSchedule(tenancies[4], currentMonthPaid: true);

        for (var i = 9; i >= 3; i--)
        {
            var periodStart = monthStart.AddMonths(-i);
            db.RentCharges.Add(new RentCharge
            {
                TenancyId = tenancies[5].Id, PeriodStart = periodStart, DueDate = periodStart, Amount = tenancies[5].RentAmount,
                AmountPaid = tenancies[5].RentAmount, PaidOn = periodStart.AddDays(1), Reconciliation = RentReconciliation.Matched,
                BankReference = $"KH06{periodStart:yyyyMM}"
            });
        }

        // ---------------------------------------------------------------- compliance
        void Cert(int propertyId, ComplianceType type, DateOnly issued, DateOnly expires, string reference, string provider,
            RenewalStatus renewal = RenewalStatus.None, int? renewalContractorId = null, DateOnly? bookedFor = null)
        {
            db.ComplianceCertificates.Add(new ComplianceCertificate
            {
                PropertyId = propertyId, Type = type, IssuedOn = issued, ExpiresOn = expires, Reference = reference,
                Provider = provider, RenewalStatus = renewal, RenewalContractorId = renewalContractorId, RenewalBookedFor = bookedFor
            });
        }

        Cert(properties[0].Id, ComplianceType.GasSafety, today.AddDays(-353), today.AddDays(12), "GS-1001-26", "Scottish Gas Certifiers");
        Cert(properties[0].Id, ComplianceType.Epc, today.AddDays(-420), today.AddDays(-420).AddYears(10), "EPC-1001-24", "Green Rock EPC");
        Cert(properties[0].Id, ComplianceType.Eicr, today.AddDays(-760), today.AddDays(-760).AddYears(5), "EICR-1001-24", "Volt & Co Electrical");

        Cert(properties[1].Id, ComplianceType.GasSafety, today.AddDays(-385), today.AddDays(-20), "GS-1002-26", "Bristol Gas Services");
        Cert(properties[1].Id, ComplianceType.Epc, today.AddDays(-2600), today.AddDays(-2600).AddYears(10), "EPC-1002-19", "West EPC");
        Cert(properties[1].Id, ComplianceType.Eicr, today.AddDays(-600), today.AddDays(-600).AddYears(5), "EICR-1002-25", "Volt & Co Electrical");

        Cert(properties[2].Id, ComplianceType.GasSafety, today.AddDays(-285), today.AddDays(80), "GS-1003-26", "Capital Gas Safety");
        Cert(properties[2].Id, ComplianceType.Epc, today.AddDays(-900), today.AddDays(-900).AddYears(10), "EPC-1003-24", "Lothian EPC");

        Cert(properties[3].Id, ComplianceType.GasSafety, today.AddDays(-340), today.AddDays(25), "GS-1004-26", "Manchester Gas Co");
        Cert(properties[3].Id, ComplianceType.Epc, today.AddDays(-700), today.AddDays(-700).AddYears(10), "EPC-1004-24", "Green Rock EPC");
        Cert(properties[3].Id, ComplianceType.Eicr, today.AddDays(-500), today.AddDays(-500).AddYears(5), "EICR-1004-25", "Volt & Co Electrical");

        Cert(properties[4].Id, ComplianceType.GasSafety, today.AddDays(-225), today.AddDays(140), "GS-1005-26", "London Gas Safe");
        Cert(properties[4].Id, ComplianceType.Epc, today.AddDays(-3690), today.AddDays(-40), "EPC-1005-16", "Capital EPC");
        Cert(properties[4].Id, ComplianceType.Eicr, today.AddDays(-280), today.AddDays(1500), "EICR-1005-26", "Volt & Co Electrical");

        Cert(properties[5].Id, ComplianceType.GasSafety, today.AddDays(-200), today.AddDays(165), "GS-1006-26", "Yorkshire Gas Services");
        Cert(properties[5].Id, ComplianceType.Epc, today.AddDays(-800), today.AddDays(-800).AddYears(10), "EPC-1006-24", "West EPC");
        Cert(properties[5].Id, ComplianceType.Eicr, today.AddDays(-400), today.AddDays(-400).AddYears(5), "EICR-1006-25", "Volt & Co Electrical");

        Cert(properties[6].Id, ComplianceType.GasSafety, today.AddDays(-330), today.AddDays(400), "GS-1007-26", "Scottish Gas Certifiers");
        Cert(properties[6].Id, ComplianceType.Epc, today.AddDays(-250), today.AddDays(3400), "EPC-1007-26", "Green Rock EPC");
        Cert(properties[6].Id, ComplianceType.Eicr, today.AddDays(-180), today.AddDays(1600), "EICR-1007-26", "Volt & Co Electrical");

        Cert(properties[7].Id, ComplianceType.GasSafety, today.AddDays(-347), today.AddDays(18), "GS-1008-26", "Bristol Gas Services");
        Cert(properties[7].Id, ComplianceType.Epc, today.AddDays(-300), today.AddDays(3300), "EPC-1008-26", "West EPC");
        Cert(properties[7].Id, ComplianceType.Eicr, today.AddDays(-150), today.AddDays(1600), "EICR-1008-26", "Volt & Co Electrical");
        Cert(properties[7].Id, ComplianceType.HmoLicence, today.AddDays(-325), today.AddDays(40), "HMO-BCC-88421", "Bristol City Council");
        await db.SaveChangesAsync(ct);

        // ---------------------------------------------------------------- maintenance
        var tickets = new List<MaintenanceTicket>
        {
            new()
            {
                Reference = "JR-2041", PropertyId = properties[0].Id, TenantId = tenants[0].Id, Title = "Leaking kitchen tap",
                Description = "The kitchen mixer tap has started dripping constantly and the base is loose. A bowl is catching the water.",
                Category = MaintenanceCategory.Plumbing, Urgency = Urgency.Routine, Status = MaintenanceStatus.Assigned,
                AiSummary = "Persistent drip from a kitchen mixer tap with a loose base. Likely a worn cartridge or seating; no water escaping the unit.",
                AiSuggestedUrgency = Urgency.Routine, AiReason = "Contained leak, tenant able to catch water, no risk to electrics or structure.",
                AccessInstructions = "Collect keys from the agency office at 14 Bothwell Street, Glasgow G2 6TS. Please sign the key register.",
                AssignedContractorId = contractors[0].Id, QuoteAmount = 85m, QuoteApproved = true,
                QuoteNotes = "Replace cartridge and reseat tap. Parts included.",
                CreatedAt = now.AddDays(-4), UpdatedAt = now.AddDays(-1)
            },
            new()
            {
                Reference = "JR-2042", PropertyId = properties[1].Id, TenantId = tenants[1].Id, Title = "Boiler firing then locking out",
                Description = "The boiler runs for a few minutes, then the pressure drops and it locks out. No hot water since yesterday evening.",
                Category = MaintenanceCategory.Heating, Urgency = Urgency.Urgent, Status = MaintenanceStatus.AwaitingApproval,
                AiSummary = "Boiler short-cycles and locks out with pressure loss. No hot water. Likely diverter valve or pressure-related fault; engineer visit needed.",
                AiSuggestedUrgency = Urgency.Urgent, AiReason = "No hot water for a household with a young child; heating still partially working.",
                AccessInstructions = "Tenant at home after 5pm on weekdays. Call on arrival.",
                AssignedContractorId = contractors[1].Id, QuoteAmount = 320m, QuoteApproved = null,
                QuoteNotes = "Diverter valve replacement plus system pressure check. Parts and labour.",
                CreatedAt = now.AddDays(-2), UpdatedAt = now.AddHours(-20)
            },
            new()
            {
                Reference = "JR-2043", PropertyId = properties[3].Id, TenantId = tenants[2].Id, Title = "Damp patch spreading in bedroom",
                Description = "A damp patch on the bedroom wall has grown over the last month. It smells musty and the paint is bubbling near the window.",
                Category = MaintenanceCategory.Damp, Urgency = Urgency.Urgent, Status = MaintenanceStatus.Triaged,
                AiSummary = "Growing damp patch with bubbling paint beside a window. Pattern suggests water ingress around the window frame rather than internal plumbing.",
                AiSuggestedUrgency = Urgency.Urgent, AiReason = "Active spread and possible ingress; needs inspection before winter. Not an immediate emergency.",
                CreatedAt = now.AddDays(-1), UpdatedAt = now.AddHours(-18)
            },
            new()
            {
                Reference = "JR-2044", PropertyId = properties[5].Id, TenantId = tenants[3].Id, Title = "Front door lock very stiff",
                Description = "The front door lock has become difficult to turn from outside. It still works but needs a firm push and turn.",
                Category = MaintenanceCategory.Locks, Urgency = Urgency.Routine, Status = MaintenanceStatus.New,
                CreatedAt = now.AddHours(-6), UpdatedAt = now.AddHours(-6)
            },
            new()
            {
                Reference = "JR-2045", PropertyId = properties[7].Id, TenantId = tenants[4].Id, Title = "Bathroom extractor fan not working",
                Description = "The extractor fan in the main bathroom has stopped running. There is condensation on the walls after showers.",
                Category = MaintenanceCategory.Electrical, Urgency = Urgency.Routine, Status = MaintenanceStatus.InProgress,
                AiSummary = "Extractor fan not running; condensation build-up in a bathroom. Likely fan motor or isolator fault.",
                AiSuggestedUrgency = Urgency.Routine, AiReason = "No immediate safety issue, but damp risk if left unresolved.",
                AssignedContractorId = contractors[2].Id, QuoteAmount = 140m, QuoteApproved = true,
                QuoteNotes = "Replace fan unit and check isolator.",
                CreatedAt = now.AddDays(-5), UpdatedAt = now.AddHours(-2)
            },
            new()
            {
                Reference = "JR-2046", PropertyId = properties[0].Id, TenantId = tenants[0].Id, Title = "Water coming through ceiling",
                Description = "Water started coming through the living room ceiling from the flat above. Buckets are in place.",
                Category = MaintenanceCategory.Plumbing, Urgency = Urgency.Emergency, Status = MaintenanceStatus.Completed,
                AiSummary = "Active water ingress from the flat above into a living room ceiling. Treated as an emergency; upstairs supply isolated.",
                AiSuggestedUrgency = Urgency.Emergency, AiReason = "Active water ingress with risk to electrics and structure.",
                AssignedContractorId = contractors[0].Id, QuoteAmount = 240m, QuoteApproved = true,
                AccessInstructions = "Tenant at home. Keys held by tenant.",
                CreatedAt = now.AddDays(-12), UpdatedAt = now.AddDays(-10), CompletedAt = now.AddDays(-10),
                CompletionNotes = "Overflow from the upstairs bathroom isolated and repaired. Ceiling dried and stain-treated; decoration booked."
            },
            new()
            {
                Reference = "JR-2047", PropertyId = properties[2].Id, Title = "Cold radiator in second bedroom",
                Description = "Noticed during the pre-marketing inspection: the radiator in the second bedroom is cold at the bottom.",
                Category = MaintenanceCategory.Heating, Urgency = Urgency.Routine, Status = MaintenanceStatus.New,
                CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-2)
            },
            new()
            {
                Reference = "JR-2048", PropertyId = properties[4].Id, Title = "Hallway redecoration after check-out",
                Description = "Turnaround work: fill and repaint the hallway walls and woodwork after the previous tenancy.",
                Category = MaintenanceCategory.Other, Urgency = Urgency.Routine, Status = MaintenanceStatus.Quoted,
                QuoteAmount = 480m, QuoteApproved = null, QuoteNotes = "Two coats, materials included.",
                CreatedAt = now.AddDays(-6), UpdatedAt = now.AddDays(-4), AccessInstructions = "Keys held in the office key vault."
            }
        };
        db.MaintenanceTickets.AddRange(tickets);
        await db.SaveChangesAsync(ct);

        // ---------------------------------------------------------------- viewings
        var tomorrow = DateTime.Today.AddDays(1);
        db.Viewings.AddRange(
            new Viewing
            {
                PropertyId = properties[2].Id, ApplicantName = "Amy Li", ApplicantEmail = "amy.li@example.com", ApplicantPhone = "07700 900611",
                ScheduledAt = tomorrow.AddHours(10).AddMinutes(30), Status = ViewingStatus.Confirmed, PrescreenStatus = PrescreenStatus.Passed,
                AgentNotes = "Pre-screen complete. Budget confirmed at £1,500 pcm, move date flexible."
            },
            new Viewing
            {
                PropertyId = properties[6].Id, ApplicantName = "Tomasz Nowak", ApplicantEmail = "tomasz.nowak@example.com", ApplicantPhone = "07700 900622",
                ScheduledAt = tomorrow.AddHours(14), Status = ViewingStatus.Requested, PrescreenStatus = PrescreenStatus.Invited,
                AgentNotes = "Rightmove enquiry. Pre-screen link sent this morning."
            },
            new Viewing
            {
                PropertyId = properties[2].Id, ApplicantName = "Grace Adeyemi", ApplicantEmail = "grace.adeyemi@example.com", ApplicantPhone = "07700 900633",
                ScheduledAt = DateTime.Today.AddDays(3).AddHours(12), Status = ViewingStatus.Confirmed, PrescreenStatus = PrescreenStatus.Passed,
                AgentNotes = "Second viewing with partner."
            },
            new Viewing
            {
                PropertyId = properties[0].Id, ApplicantName = "Liam O'Connor", ApplicantEmail = "liam.oconnor@example.com", ApplicantPhone = "07700 900366",
                ScheduledAt = DateTime.Today.AddDays(-2).AddHours(16), Status = ViewingStatus.Completed, PrescreenStatus = PrescreenStatus.Passed,
                Feedback = "Liked the flat and the location. Asked about parking permits and a slightly earlier move-in date."
            },
            new Viewing
            {
                PropertyId = properties[6].Id, ApplicantName = "Priya Nair", ApplicantEmail = "priya.nair@example.com", ApplicantPhone = "07700 900644",
                ScheduledAt = DateTime.Today.AddDays(5).AddHours(9).AddMinutes(30), Status = ViewingStatus.Requested, PrescreenStatus = PrescreenStatus.NotStarted,
            },
            new Viewing
            {
                PropertyId = properties[4].Id, ApplicantName = "Hannah Price", ApplicantEmail = "hannah.price@example.com", ApplicantPhone = "07700 900655",
                ScheduledAt = DateTime.Today.AddDays(-5).AddHours(11), Status = ViewingStatus.Completed, PrescreenStatus = PrescreenStatus.Passed,
                Feedback = "Happy with the flat but wants to compare with a second property before offering."
            }
        );

        // ---------------------------------------------------------------- enquiries
        db.Enquiries.AddRange(
            new Enquiry
            {
                PropertyId = properties[2].Id, Name = "Amy Li", Email = "amy.li@example.com", Phone = "07700 900611",
                Message = "Is the flat still available? Could you let me know the rent and whether it is available from December?",
                Source = EnquirySource.Website, ReceivedAt = now.AddHours(-2), Status = EnquiryStatus.New
            },
            new Enquiry
            {
                PropertyId = properties[6].Id, Name = "Tomasz Nowak", Email = "tomasz.nowak@example.com", Phone = "07700 900622",
                Message = "I'm interested in the one-bedroom flat in Partick. Is it still available and can I arrange a viewing this week?",
                Source = EnquirySource.Rightmove, ReceivedAt = now.AddHours(-5), Status = EnquiryStatus.New
            },
            new Enquiry
            {
                PropertyId = properties[2].Id, Name = "Grace Adeyemi", Email = "grace.adeyemi@example.com", Phone = "07700 900633",
                Message = "We loved the listing for Bellevue Crescent. Is there any flexibility on the move-in date?",
                Source = EnquirySource.FindPortal, ReceivedAt = now.AddHours(-6), Status = EnquiryStatus.Replied
            },
            new Enquiry
            {
                PropertyId = properties[1].Id, Name = "David Chen", Email = "david.chen@example.com",
                Message = "Is 48 Richmond Road still available? Looking for a three-bed from January.",
                Source = EnquirySource.Rightmove, ReceivedAt = now.AddDays(-1), Status = EnquiryStatus.New
            },
            new Enquiry
            {
                Name = "Ruth Kelly", Email = "ruth.kelly@example.com", Phone = "07700 900677",
                Message = "Do you manage properties in the south side of Glasgow? I'm looking for a letting agent for a two-bed flat.",
                Source = EnquirySource.Phone, ReceivedAt = now.AddDays(-2), Status = EnquiryStatus.Replied,
                Notes = "Owner enquiry, not a tenant. Booked a call for Thursday."
            }
        );

        // ---------------------------------------------------------------- key vault
        var keys = new List<KeyItem>();
        var serial = 1;
        foreach (var property in properties)
        {
            keys.Add(new KeyItem
            {
                Label = $"House key — {property.Reference}", Type = KeyType.HouseKey, Serial = $"KH-K{serial++:D4}",
                PropertyId = property.Id, Notes = "Held in the office key safe."
            });
            keys.Add(new KeyItem
            {
                Label = $"Building fob — {property.Reference}", Type = KeyType.Fob, Serial = $"KH-F{serial++:D4}",
                PropertyId = property.Id, Notes = "Held in the office key safe."
            });
        }

        var prop1Key = keys.First(k => k.PropertyId == properties[0].Id && k.Type == KeyType.HouseKey);
        prop1Key.IsCheckedOut = true;
        prop1Key.CheckedOutToName = "Dale Kerr (Kerr Facilities)";
        prop1Key.CheckedOutAt = now.AddDays(-2);
        prop1Key.CheckOutReason = "Kitchen tap repair — JR-2041";
        prop1Key.DueBackAt = now.AddDays(1);

        var prop3Key = keys.First(k => k.PropertyId == properties[2].Id && k.Type == KeyType.HouseKey);
        prop3Key.IsCheckedOut = true;
        prop3Key.CheckedOutToName = "Priya Shah (viewings)";
        prop3Key.CheckedOutAt = now.AddHours(-20);
        prop3Key.CheckOutReason = "Viewings at Bellevue Crescent";
        prop3Key.DueBackAt = now.AddDays(2);

        var prop8Fob = keys.First(k => k.PropertyId == properties[7].Id && k.Type == KeyType.Fob);
        prop8Fob.IsCheckedOut = true;
        prop8Fob.CheckedOutToName = "Bright & Clean";
        prop8Fob.CheckedOutAt = now.AddDays(-6);
        prop8Fob.CheckOutReason = "Turnaround clean";
        prop8Fob.DueBackAt = now.AddDays(-2);

        db.KeyItems.AddRange(keys);

        // ---------------------------------------------------------------- CRM
        db.ContactNotes.AddRange(
            new ContactNote
            {
                ContactType = ContactType.Owner, ContactName = "James Whitfield", ContactEmail = "james@whitfieldproperty.example",
                Channel = ContactChannel.Phone, Summary = "Quarterly portfolio check-in. Happy with activity at Bellevue Crescent; asked for the Q3 statement.",
                OccurredAt = now.AddDays(-1), CreatedBy = "Priya Shah", IsFollowUp = true, FollowUpOn = today.AddDays(2)
            },
            new ContactNote
            {
                ContactType = ContactType.Tenant, ContactName = "Sarah Connolly", ContactEmail = "tenant@keyhaven.co.uk",
                Channel = ContactChannel.Email, Summary = "Confirmed the kitchen tap repair is with Dale. No appointment time recorded yet, so she will be updated.",
                OccurredAt = now.AddHours(-3), CreatedBy = "Priya Shah", IsFollowUp = true, FollowUpOn = today.AddDays(1),
                PropertyId = properties[0].Id
            },
            new ContactNote
            {
                ContactType = ContactType.Tenant, ContactName = "Daniel Okafor", ContactEmail = "daniel.okafor@example.com",
                Channel = ContactChannel.Email, Summary = "Sent a rent reminder covering the outstanding balance. Tenant has asked to discuss a payment plan.",
                OccurredAt = now.AddDays(-3), CreatedBy = "Alex Morgan", IsFollowUp = true, FollowUpOn = today, PropertyId = properties[1].Id
            },
            new ContactNote
            {
                ContactType = ContactType.Contractor, ContactName = "Dale Kerr", ContactEmail = "contractor@keyhaven.co.uk",
                Channel = ContactChannel.Phone, Summary = "Confirmed he can attend the Clarence Drive tap this week. Will collect keys from the office.",
                OccurredAt = now.AddDays(-1), CreatedBy = "Priya Shah", PropertyId = properties[0].Id
            },
            new ContactNote
            {
                ContactType = ContactType.Applicant, ContactName = "Amy Li", ContactEmail = "amy.li@example.com",
                Channel = ContactChannel.Email, Summary = "Enquiry received for Bellevue Crescent. Viewing booked for tomorrow at 10:30.",
                OccurredAt = now.AddHours(-2), CreatedBy = "Keyhaven AI", PropertyId = properties[2].Id
            },
            new ContactNote
            {
                ContactType = ContactType.Owner, ContactName = "Fiona MacLeod", ContactEmail = "fiona@argyllestates.example",
                Channel = ContactChannel.Meeting, Summary = "Portfolio review: Bristol gas certificate has lapsed and needs renewal urgently. Agreed to instruct Heatwise.",
                OccurredAt = now.AddDays(-4), CreatedBy = "Alex Morgan", IsFollowUp = true, FollowUpOn = today.AddDays(1)
            },
            new ContactNote
            {
                ContactType = ContactType.Tenant, ContactName = "Chloe Bennett", ContactEmail = "chloe.bennett@example.com",
                Channel = ContactChannel.Note, Summary = "Extractor fan part ordered; Volt & Co due back on site.",
                OccurredAt = now.AddDays(-2), CreatedBy = "Priya Shah", PropertyId = properties[7].Id
            },
            new ContactNote
            {
                ContactType = ContactType.Contractor, ContactName = "Heatwise Scotland", ContactEmail = "jobs@heatwise.example",
                Channel = ContactChannel.Email, Summary = "Quote received for the Bristol boiler: £320 including parts. Awaiting owner approval.",
                OccurredAt = now.AddDays(-1), CreatedBy = "Priya Shah", PropertyId = properties[1].Id
            }
        );

        // ---------------------------------------------------------------- inbox
        var inbox = new List<InboxMessage>
        {
            new()
            {
                FromName = "Amy Li", FromEmail = "amy.li@example.com", Subject = "Is 7 Bellevue Crescent still available?",
                Body = "Hi there,\n\nI'm interested in the flat at 7 Bellevue Crescent. Is it still available, and could you let me know the rent?\n\nThanks,\nAmy",
                ReceivedAt = now.AddHours(-2), LinkedPropertyId = properties[2].Id
            },
            new()
            {
                FromName = "Sarah Connolly", FromEmail = "tenant@keyhaven.co.uk", Subject = "Any news on the kitchen tap?",
                Body = "Hi Priya,\n\nJust checking whether there's an update on the leaking kitchen tap. It's still dripping into the bowl.\n\nThanks,\nSarah",
                ReceivedAt = now.AddHours(-1), LinkedPropertyId = properties[0].Id, LinkedTenancyId = tenancies[0].Id, LinkedTicketId = tickets[0].Id
            },
            new()
            {
                FromName = "Dale Kerr", FromEmail = "contractor@keyhaven.co.uk", Subject = "Access details for the tap repair",
                Body = "Morning,\n\nCan you confirm the access arrangements for 12 Clarence Drive? I can attend Thursday morning.\n\nDale",
                ReceivedAt = now.AddMinutes(-40), LinkedPropertyId = properties[0].Id, LinkedTicketId = tickets[0].Id
            },
            new()
            {
                FromName = "James Whitfield", FromEmail = "james@whitfieldproperty.example", Subject = "Statement for the last quarter",
                Body = "Hi Priya,\n\nCould you send me the statement for the last quarter when you get a chance?\n\nBest,\nJames",
                ReceivedAt = now.AddDays(-1), LinkedOwnerId = owners[0].Id
            },
            new()
            {
                FromName = "Daniel Okafor", FromEmail = "daniel.okafor@example.com", Subject = "Rent balance",
                Body = "Hi,\n\nI think I've fallen behind with the rent. Can you tell me what I owe and whether we can arrange a plan?\n\nDaniel",
                ReceivedAt = now.AddHours(-3), LinkedPropertyId = properties[1].Id, LinkedTenancyId = tenancies[1].Id
            },
            new()
            {
                FromName = "David Chen", FromEmail = "david.chen@example.com", Subject = "48 Richmond Road — still available?",
                Body = "Hello,\n\nIs 48 Richmond Road still available? I'm looking for a three-bedroom house from January.\n\nThanks,\nDavid",
                ReceivedAt = now.AddHours(-5), LinkedPropertyId = properties[1].Id
            },
            new()
            {
                FromName = "Grace Adeyemi", FromEmail = "grace.adeyemi@example.com", Subject = "Reschedule Saturday viewing",
                Body = "Hi,\n\nSomething has come up on Saturday. Could we move our viewing at 7 Bellevue Crescent to another time?\n\nThanks,\nGrace",
                ReceivedAt = now.AddMinutes(-30), LinkedPropertyId = properties[2].Id
            },
            new()
            {
                FromName = "Mark Stevens", FromEmail = "mark@reachmoremarketing.example", Subject = "Quick question about your marketing",
                Body = "Hi, I help letting agencies generate more leads through paid social. Would you have 15 minutes for a call this week?",
                ReceivedAt = now.AddDays(-1)
            }
        };
        db.InboxMessages.AddRange(inbox);

        await db.SaveChangesAsync(ct);
    }
}
