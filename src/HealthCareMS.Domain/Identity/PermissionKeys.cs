namespace HealthCareMS.Domain.Identity;

public static class PermissionKeys
{
    public static IReadOnlyCollection<string> All => System.All
        .Concat(Tenant.All)
        .Concat(Appointment.All)
        .Concat(Consultation.All)
        .Concat(Pharmacy.All)
        .Concat(Lab.All)
        .Concat(Payment.All)
        .Concat(Patient.All)
        .Concat(Doctor.All)
        .ToArray();

    public static class System
    {
        public const string SuperAdminAll = "system.superadmin.*";
        public const string TenantsCreate = "system.tenants.create";
        public const string TenantsActivate = "system.tenants.activate";
        public const string UsersViewAll = "system.users.view_all";
        public const string ReportsGlobal = "system.reports.global";

        public static readonly string[] All =
        [
            SuperAdminAll,
            TenantsCreate,
            TenantsActivate,
            UsersViewAll,
            ReportsGlobal
        ];
    }

    public static class Tenant
    {
        public const string UsersCreate = "tenant.users.create";
        public const string UsersAssignRole = "tenant.users.assign_role";
        public const string RolesCreate = "tenant.roles.create";
        public const string PermissionsGrant = "tenant.permissions.grant";
        public const string SettingsUpdate = "tenant.settings.update";

        public static readonly string[] All =
        [
            UsersCreate,
            UsersAssignRole,
            RolesCreate,
            PermissionsGrant,
            SettingsUpdate
        ];
    }

    public static class Appointment
    {
        public const string View = "appointment.view";
        public const string Book = "appointment.book";
        public const string Confirm = "appointment.confirm";
        public const string Complete = "appointment.complete";
        public const string Cancel = "appointment.cancel";

        public static readonly string[] All = [View, Book, Confirm, Complete, Cancel];
    }

    public static class Consultation
    {
        public const string VideoStart = "consultation.video.start";
        public const string PrescriptionCreate = "consultation.prescription.create";

        public static readonly string[] All = [VideoStart, PrescriptionCreate];
    }

    public static class Pharmacy
    {
        public const string MedicinesView = "pharmacy.medicines.view";
        public const string MedicinesCreate = "pharmacy.medicines.create";
        public const string MedicinesEdit = "pharmacy.medicines.edit";
        public const string StockView = "pharmacy.stock.view";
        public const string StockAdjust = "pharmacy.stock.adjust";
        public const string OrdersView = "pharmacy.orders.view";
        public const string OrdersProcess = "pharmacy.orders.process";
        public const string Dispense = "pharmacy.dispense";
        public const string ReportsView = "pharmacy.reports.view";
        public const string GatewaysJazzCash = "pharmacy.gateways.jazzcash";
        public const string GatewaysEasyPaisa = "pharmacy.gateways.easypaisa";
        public const string GatewaysStripe = "pharmacy.gateways.stripe";

        public static readonly string[] All =
        [
            MedicinesView,
            MedicinesCreate,
            MedicinesEdit,
            StockView,
            StockAdjust,
            OrdersView,
            OrdersProcess,
            Dispense,
            ReportsView,
            GatewaysJazzCash,
            GatewaysEasyPaisa,
            GatewaysStripe
        ];
    }

    public static class Lab
    {
        public const string TestsView = "lab.tests.view";
        public const string BookingCreate = "lab.booking.create";
        public const string SampleCollect = "lab.sample.collect";
        public const string ResultsEntry = "lab.results.entry";
        public const string ResultsValidate = "lab.results.validate";
        public const string ResultsRelease = "lab.results.release";
        public const string ReportsDownload = "lab.reports.download";

        public static readonly string[] All =
        [
            TestsView,
            BookingCreate,
            SampleCollect,
            ResultsEntry,
            ResultsValidate,
            ResultsRelease,
            ReportsDownload
        ];
    }

    public static class Payment
    {
        public const string InvoicesView = "payment.invoices.view";
        public const string RefundInitiate = "payment.refund.initiate";

        public static readonly string[] All = [InvoicesView, RefundInitiate];
    }

    public static class Patient
    {
        public const string RecordsViewOwn = "patient.records.view_own";
        public const string RecordsViewOthers = "patient.records.view_others";

        public static readonly string[] All = [RecordsViewOwn, RecordsViewOthers];
    }

    public static class Doctor
    {
        public const string ScheduleManage = "doctor.schedule.manage";
        public const string Verify = "doctor.verify";

        public static readonly string[] All = [ScheduleManage, Verify];
    }
}
