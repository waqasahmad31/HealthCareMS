namespace HealthCareMS.Infrastructure.Configuration;

public static class NavigationDefaults
{
    public const string SettingKey = "Platform.Navigation.MenuConfigJson";

    public const string ConfigurationJson =
        """
        {"groups":[{"key":"general","sortOrder":10,"labels":{"en":"General","ur":"جنرل"},"items":[{"key":"dashboard","label":{"en":"Dashboard","ur":"ڈیش بورڈ"},"icon":"dashboard","route":"","sortOrder":10},{"key":"notifications","label":{"en":"Notifications","ur":"نوٹیفیکیشنز"},"icon":"notifications","route":"notifications","sortOrder":20}]},{"key":"admin","sortOrder":20,"labels":{"en":"Admin","ur":"ایڈمن"},"items":[{"key":"tenants","label":{"en":"Tenants","ur":"ٹیننٹس"},"icon":"tenants","route":"tenants","sortOrder":10,"requiredPermissions":["system.tenants.create"]},{"key":"doctors","label":{"en":"Doctors","ur":"ڈاکٹرز"},"icon":"doctors","route":"admin/doctors","sortOrder":20,"requiredPermissions":["doctor.verify"]},{"key":"config","label":{"en":"Configuration","ur":"کنفیگریشن"},"icon":"config","route":"admin/system-configuration","sortOrder":30,"requiredPermissions":["tenant.settings.update"]}]},{"key":"operations","sortOrder":30,"labels":{"en":"Operations","ur":"آپریشنز"},"items":[{"key":"doctor-portal","label":{"en":"Doctor Portal","ur":"ڈاکٹر پورٹل"},"icon":"doctor-portal","route":"portal/doctor","sortOrder":10,"requiredPermissions":["doctor.schedule.manage"]},{"key":"patient-portal","label":{"en":"Patient Portal","ur":"پیشنٹ پورٹل"},"icon":"patient-portal","route":"portal/patient","sortOrder":20,"requiredPermissions":["patient.records.view_own"]},{"key":"pharmacy","label":{"en":"Pharmacy","ur":"فارمیسی"},"icon":"pharmacy","route":"pharmacy","sortOrder":30,"requiredPermissions":["pharmacy.medicines.view"]},{"key":"lab","label":{"en":"Laboratory","ur":"لیبارٹری"},"icon":"lab","route":"laboratory","sortOrder":40,"requiredPermissions":["lab.tests.view"]}]}]}
        """;
}
