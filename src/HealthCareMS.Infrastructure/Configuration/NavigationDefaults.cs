namespace HealthCareMS.Infrastructure.Configuration;

public static class NavigationDefaults
{
    public const string SettingKey = "Platform.Navigation.MenuConfigJson";

    public const string ConfigurationJson =
        """
        {
          "groups": [
            {
              "key": "general",
              "sortOrder": 10,
              "labels": {
                "en": "General",
                "ur": "\u062c\u0646\u0631\u0644"
              },
              "items": [
                {
                  "key": "dashboard",
                  "label": {
                    "en": "Dashboard",
                    "ur": "\u0688\u06cc\u0634 \u0628\u0648\u0631\u0688"
                  },
                  "icon": "dashboard",
                  "route": "",
                  "sortOrder": 10
                },
                {
                  "key": "notifications",
                  "label": {
                    "en": "Notifications",
                    "ur": "\u0646\u0648\u0679\u06cc\u0641\u06cc\u06a9\u06cc\u0634\u0646\u0632"
                  },
                  "icon": "notifications",
                  "route": "notifications",
                  "sortOrder": 20
                }
              ]
            },
            {
              "key": "admin",
              "sortOrder": 20,
              "labels": {
                "en": "Admin",
                "ur": "\u0627\u06cc\u0688\u0645\u0646"
              },
              "items": [
                {
                  "key": "tenants",
                  "label": {
                    "en": "Tenants",
                    "ur": "\u0679\u06cc\u0646\u0646\u0679\u0633"
                  },
                  "icon": "tenants",
                  "route": "tenants",
                  "sortOrder": 10,
                  "requiredPermissions": [
                    "system.tenants.create"
                  ]
                },
                {
                  "key": "doctors",
                  "label": {
                    "en": "Doctors",
                    "ur": "\u0688\u0627\u06a9\u0679\u0631\u0632"
                  },
                  "icon": "doctors",
                  "route": "admin/doctors",
                  "sortOrder": 20,
                  "requiredPermissions": [
                    "doctor.verify"
                  ]
                },
                {
                  "key": "config",
                  "label": {
                    "en": "Configuration",
                    "ur": "\u06a9\u0646\u0641\u06cc\u06af\u0631\u06cc\u0634\u0646"
                  },
                  "icon": "config",
                  "route": "admin/system-configuration",
                  "sortOrder": 30,
                  "requiredPermissions": [
                    "tenant.settings.update"
                  ]
                },
                {
                  "key": "navigation-studio",
                  "label": {
                    "en": "Navigation Studio",
                    "ur": "\u0646\u06cc\u0648\u06cc\u06af\u06cc\u0634\u0646 \u0627\u0633\u0679\u0648\u0688\u06cc\u0648"
                  },
                  "icon": "admin-tools",
                  "route": "admin/navigation-studio",
                  "sortOrder": 40,
                  "requiredPermissions": [
                    "system.users.view_all"
                  ]
                }
              ]
            },
            {
              "key": "operations",
              "sortOrder": 30,
              "labels": {
                "en": "Operations",
                "ur": "\u0622\u067e\u0631\u06cc\u0634\u0646\u0632"
              },
              "items": [
                {
                  "key": "doctor-portal",
                  "label": {
                    "en": "Doctor Portal",
                    "ur": "\u0688\u0627\u06a9\u0679\u0631 \u067e\u0648\u0631\u0679\u0644"
                  },
                  "icon": "doctor-portal",
                  "route": "portal/doctor",
                  "sortOrder": 10,
                  "requiredPermissions": [
                    "doctor.schedule.manage"
                  ]
                },
                {
                  "key": "patient-portal",
                  "label": {
                    "en": "Patient Portal",
                    "ur": "\u067e\u06cc\u0634\u0646\u0679 \u067e\u0648\u0631\u0679\u0644"
                  },
                  "icon": "patient-portal",
                  "route": "portal/patient",
                  "sortOrder": 20,
                  "requiredPermissions": [
                    "patient.records.view_own"
                  ]
                },
                {
                  "key": "payments",
                  "label": {
                    "en": "Payments",
                    "ur": "\u067e\u06cc\u0645\u0646\u0679\u0633"
                  },
                  "icon": "payments",
                  "route": "payments",
                  "sortOrder": 30,
                  "requiredPermissions": [
                    "payment.invoices.view"
                  ]
                },
                {
                  "key": "pharmacy",
                  "label": {
                    "en": "Pharmacy",
                    "ur": "\u0641\u0627\u0631\u0645\u06cc\u0633\u06cc"
                  },
                  "icon": "pharmacy",
                  "route": "pharmacy",
                  "sortOrder": 40,
                  "requiredPermissions": [
                    "pharmacy.medicines.view"
                  ]
                },
                {
                  "key": "pharmacy-reports",
                  "label": {
                    "en": "Pharmacy Reports",
                    "ur": "\u0641\u0627\u0631\u0645\u06cc\u0633\u06cc \u0631\u067e\u0648\u0631\u0679\u0633"
                  },
                  "icon": "reports",
                  "route": "pharmacy/reports",
                  "sortOrder": 50,
                  "requiredPermissions": [
                    "pharmacy.reports.view"
                  ]
                },
                {
                  "key": "lab",
                  "label": {
                    "en": "Laboratory",
                    "ur": "\u0644\u06cc\u0628\u0627\u0631\u0679\u0631\u06cc"
                  },
                  "icon": "lab",
                  "route": "laboratory",
                  "sortOrder": 60,
                  "requiredPermissions": [
                    "lab.tests.view"
                  ]
                },
                {
                  "key": "lab-workflow",
                  "label": {
                    "en": "Lab Workflow",
                    "ur": "\u0644\u06cc\u0628 \u0648\u0631\u06a9 \u0641\u0644\u0648"
                  },
                  "icon": "lab",
                  "route": "lab-workflow",
                  "sortOrder": 70,
                  "requiredPermissions": [
                    "lab.sample.collect",
                    "lab.results.entry",
                    "lab.results.validate",
                    "lab.results.release",
                    "lab.reports.download"
                  ]
                }
              ]
            },
            {
              "key": "insights",
              "sortOrder": 40,
              "labels": {
                "en": "Insights",
                "ur": "\u0627\u0646\u0633\u0627\u0626\u0679\u0633"
              },
              "items": [
                {
                  "key": "timeline",
                  "label": {
                    "en": "Health Timeline",
                    "ur": "\u06c1\u06cc\u0644\u062a\u06be \u0679\u0627\u0626\u0645 \u0644\u0627\u0626\u0646"
                  },
                  "icon": "timeline",
                  "route": "timeline",
                  "sortOrder": 10,
                  "requiredPermissions": [
                    "patient.records.view_own",
                    "patient.records.view_others"
                  ]
                },
                {
                  "key": "analytics",
                  "label": {
                    "en": "Analytics",
                    "ur": "\u0627\u06cc\u0646\u0627\u0644\u06cc\u0679\u06a9\u0633"
                  },
                  "icon": "analytics",
                  "route": "analytics",
                  "sortOrder": 20,
                  "requiredPermissions": [
                    "system.reports.global"
                  ]
                },
                {
                  "key": "doctor-discovery",
                  "label": {
                    "en": "Doctor Discovery",
                    "ur": "\u0688\u0627\u06a9\u0679\u0631 \u0688\u0633\u06a9\u0648\u0631\u06cc"
                  },
                  "icon": "discovery",
                  "route": "doctor-discovery",
                  "sortOrder": 30
                }
              ]
            },
            {
              "key": "workspace",
              "sortOrder": 50,
              "labels": {
                "en": "Workspace",
                "ur": "\u0648\u0631\u06a9 \u0627\u0633\u067e\u06cc\u0633"
              },
              "items": [
                {
                  "key": "security",
                  "label": {
                    "en": "Security",
                    "ur": "\u0633\u06a9\u06cc\u0648\u0631\u0679\u06cc"
                  },
                  "icon": "security",
                  "route": "security",
                  "sortOrder": 10
                },
                {
                  "key": "help",
                  "label": {
                    "en": "Help Center",
                    "ur": "\u06c1\u06cc\u0644\u067e \u0633\u06cc\u0646\u0679\u0631"
                  },
                  "icon": "help",
                  "route": "help",
                  "sortOrder": 20
                }
              ]
            }
          ]
        }
        """;
}
