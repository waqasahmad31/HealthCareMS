window.healthCareCulture = (function () {
  const key = "HealthCareMS.Culture";
  const rtlCulture = "ur";
  const translatableAttributes = ["placeholder", "title", "aria-label"];
  const originalText = new WeakMap();
  const originalAttributes = new WeakMap();
  let observerStarted = false;
  let isApplying = false;

  const translationsUr = Object.freeze({
    "Enterprise Suite": "انٹرپرائز سوٹ",
    "Navigation menu": "نیویگیشن مینو",
    "Navigation could not be loaded.": "نیویگیشن لوڈ نہیں ہو سکی۔",
    "Asia/Karachi": "ایشیا/کراچی",
    "Super Admin Workspace": "سپر ایڈمن ورک اسپیس",
    "Notifications": "اطلاعات",
    "Theme": "تھیم",
    "Dark Mode": "گہرا انداز",
    "Light Mode": "روشن انداز",
    "Logout": "لاگ آؤٹ",
    "Authentication": "تصدیق",
    "Sign In": "سائن اِن",
    "Email": "ای میل",
    "Password": "پاس ورڈ",
    "Login": "لاگ اِن",
    "Login failed.": "لاگ اِن ناکام رہا۔",
    "API unavailable.": "API دستیاب نہیں ہے۔",
    "Not Found": "صفحہ نہیں ملا",
    "Sorry, the content you are looking for does not exist.": "معذرت، آپ جس مواد کو تلاش کر رہے ہیں وہ موجود نہیں ہے۔",
    "Enterprise Healthcare Management": "انٹرپرائز ہیلتھ کیئر مینجمنٹ",
    "Operations Dashboard": "آپریشنز ڈیش بورڈ",
    "Open Analytics": "اینالیٹکس کھولیں",
    "Manage Tenants": "ٹیننٹس منظم کریں",
    "Active Tenants": "فعال ٹیننٹس",
    "Patients": "مریض",
    "Doctors": "ڈاکٹرز",
    "Revenue": "ریونیو",
    "Ready for onboarding": "آن بورڈنگ کے لیے تیار",
    "Unified patient health view": "مریض کی یکجا صحتی منظر",
    "Recommendation engine enabled": "ریکمینڈیشن انجن فعال",
    "Checkout + refunds + analytics": "چیک آؤٹ + ریفنڈز + اینالیٹکس",
    "Today": "آج",
    "Live": "لائیو",
    "Payments center": "پیمنٹس سینٹر",
    "JazzCash, EasyPaisa, Stripe checkout with invoice/refund simulation": "جازکیش، ایزی پیسہ، اور اسٹرائپ چیک آؤٹ مع انوائس اور ریفنڈ سمولیشن",
    "Lab workflow": "لیب ورک فلو",
    "Home collection, result entry, validation, release, QR reports": "ہوم کلیکشن، رزلٹ انٹری، ویلیڈیشن، ریلیز، اور QR رپورٹس",
    "Analytics center": "اینالیٹکس سینٹر",
    "Cross-module revenue, utilization, TAT, fulfillment": "کراس ماڈیول ریونیو، استعمال، ٹرن اَراؤنڈ ٹائم، اور فل فلمنٹ",
    "Security center": "سیکیورٹی سینٹر",
    "Sessions, login history, 2FA controls": "سیشنز، لاگ اِن ہسٹری، اور 2FA کنٹرولز",
    "Modules": "ماڈیولز",
    "Enabled": "فعال",
    "Identity": "شناخت",
    "Users, roles, permissions": "صارفین، رولز، پرمشنز",
    "Patient": "مریض",
    "Timeline + emergency QR": "ٹائم لائن + ایمرجنسی QR",
    "Doctor": "ڈاکٹر",
    "Discovery + recommendations": "ڈسکوری + سفارشات",
    "Appointment": "اپائنٹمنٹ",
    "Consultation lifecycle": "مشاورتی لائف سائیکل",
    "Pharmacy": "فارمیسی",
    "Payments + reconciliation": "پیمنٹس + ری کنسیلی ایشن",
    "Laboratory": "لیبارٹری",
    "Home collection + released reports": "ہوم کلیکشن + جاری شدہ رپورٹس",
    "Security": "سیکیورٹی",
    "Sessions + 2FA": "سیشنز + 2FA",
    "Care Journeys": "کیئر جرنیز",
    "Guided flow": "گائیڈڈ فلو",
    "Permission Baseline": "پرمشن بیس لائن",
    "Super Admin": "سپر ایڈمن",
    "Platform": "پلیٹ فارم",
    "Tenant Management": "ٹیننٹ مینجمنٹ",
    "Tenant Admin": "ٹیننٹ ایڈمن",
    "Enterprise Launchpad": "انٹرپرائز لانچ پیڈ",
    "Core Workspaces": "بنیادی ورک اسپیسز",
    "Patient Intake To Booking": "پیشنٹ ان ٹیک سے بُکنگ تک",
    "Register or search a patient, then carry context into appointment booking": "مریض کو رجسٹر یا تلاش کریں، پھر اسی سیاق کو اپائنٹمنٹ بُکنگ میں لے جائیں",
    "Doctor Readiness": "ڈاکٹر ریڈینس",
    "Credential, verify, and prepare weekly schedule coverage": "کریڈنشل، ویریفائی، اور ہفتہ وار شیڈول کوریج تیار کریں",
    "Operational Scheduling": "آپریشنل شیڈیولنگ",
    "Guide patient + doctor context into lifecycle-controlled appointments": "مریض اور ڈاکٹر کے سیاق کو لائف سائیکل کنٹرولڈ اپائنٹمنٹس میں لے جائیں",
    "Go-Live Support": "گو لائیو سپورٹ",
    "Training assets, rollout guidance, and support handover": "ٹریننگ مواد، رول آؤٹ گائیڈنس، اور سپورٹ ہینڈ اوور",
    "Payments Center": "پیمنٹس سینٹر",
    "Gateway sessions, invoices, refunds": "گیٹ وے سیشنز، انوائسز، ریفنڈز",
    "Pharmacy Reports": "فارمیسی رپورٹس",
    "Sales, valuation, reconciliation": "سیلز، ویلیو ایشن، ری کنسیلی ایشن",
    "Collection to report release": "کلیکشن سے رپورٹ ریلیز تک",
    "Health Timeline": "ہیلتھ ٹائم لائن",
    "Patient summary and emergency QR": "مریض خلاصہ اور ایمرجنسی QR",
    "Analytics Center": "اینالیٹکس سینٹر",
    "Cross-module KPIs": "کراس ماڈیول KPIز",
    "2FA, sessions, login history": "2FA، سیشنز، لاگ اِن ہسٹری",
    "Best-match recommendation engine": "بہترین میچ سفارشاتی انجن",
    "Help Center": "ہیلپ سینٹر",
    "Guides and rollout handover": "گائیڈز اور رول آؤٹ ہینڈ اوور",
    "Patients Workspace": "پیشنٹس ورک اسپیس",
    "Registry, medical history, vitals": "رجسٹری، میڈیکل ہسٹری، وائیٹلز",
    "Doctors Workspace": "ڈاکٹرز ورک اسپیس",
    "Discovery, verification, schedules": "ڈسکوری، ویریفکیشن، شیڈولز",
    "Appointments Workspace": "اپائنٹمنٹس ورک اسپیس",
    "Booking, reschedule, completion": "بُکنگ، ری شیڈیول، تکمیل",
    "S36 Help & Training": "S36 مدد اور تربیت",
    "Launch Guides & Walkthroughs": "لانچ گائیڈز اور واک تھروز",
    "User Guides": "یوزر گائیڈز",
    "Patient Journey": "مریض کا سفر",
    "Timeline, bookings, emergency QR, prescriptions.": "ٹائم لائن، بُکنگز، ایمرجنسی QR، اور نسخے۔",
    "Doctor Journey": "ڈاکٹر کا سفر",
    "Queue, consultation, lab review, best-match discovery.": "کیو، کنسلٹیشن، لیب ریویو، اور بہترین میچ ڈسکوری۔",
    "Pharmacy & Lab Ops": "فارمیسی اور لیب آپریشنز",
    "Payments, reconciliation, home collection, result release.": "پیمنٹس، ری کنسیلی ایشن، ہوم کلیکشن، اور رزلٹ ریلیز۔",
    "Security & Admin": "سیکیورٹی اور ایڈمن",
    "Sessions, 2FA, analytics, deployment readiness.": "سیشنز، 2FA، اینالیٹکس، اور ڈپلائمنٹ ریڈینس۔",
    "Walkthrough Links": "واک تھرو لنکس",
    "Go-live": "گو لائیو",
    "Gateway checkout, invoice, refund simulation.": "گیٹ وے چیک آؤٹ، انوائس، اور ریفنڈ سمولیشن۔",
    "Sales, stock valuation, reconciliation exports.": "سیلز، اسٹاک ویلیو ایشن، اور ری کنسیلی ایشن ایکسپورٹس۔",
    "Patient health timeline and PDF summary.": "مریض ہیلتھ ٹائم لائن اور PDF خلاصہ۔",
    "Global KPI dashboard and digest-ready metrics.": "گلوبل KPI ڈیش بورڈ اور ڈائجسٹ کے لیے تیار میٹرکس۔",
    "2FA setup, active sessions, login history.": "2FA سیٹ اپ، فعال سیشنز، اور لاگ اِن ہسٹری۔",
    "Recommendation engine and best-match scoring.": "ریکمینڈیشن انجن اور بہترین میچ اسکورنگ۔",
    "Deployment Pack": "ڈپلائمنٹ پیک",
    "Container topology, env vars, migration flow, release gates.": "کنٹینر ٹوپولوجی، اینوائرنمنٹ ویری ایبلز، مائیگریشن فلو، اور ریلیز گیٹس۔",
    "Daily checks, rollback path, incident pointers, hypercare workflow.": "روزانہ چیکس، رول بیک پاتھ، انسیڈنٹ پوائنٹرز، اور ہائپرکیئر ورک فلو۔",
    "PostgreSQL, API, Blazor, edge reverse proxy production stack.": "پوسٹگریس، API، بلیزر، اور ایج ریورس پراکسی پروڈکشن اسٹیک۔",
    "Automated post-deploy HTTP smoke verification script.": "خودکار پوسٹ ڈپلائمنٹ HTTP اسموک ویریفکیشن اسکرپٹ۔",
    "Validation Pack": "ویلیڈیشن پیک",
    "65 Integration Tests": "65 انٹیگریشن ٹیسٹس",
    "Advanced payments, lab release, analytics, recommendation, security, and booking consistency coverage.": "ایڈوانسڈ پیمنٹس، لیب ریلیز، اینالیٹکس، ریکمینڈیشن، سیکیورٹی، اور بُکنگ کنسسٹنسی کوریج۔",
    "5 Unit Tests": "5 یونٹ ٹیسٹس",
    "Core low-level behavior remains green after premium rollout.": "پریمیم رول آؤٹ کے بعد بنیادی لو-لیول رویہ سبز حالت میں ہے۔",
    "Role-based training and hypercare ownership model.": "رول بیسڈ ٹریننگ اور ہائپرکیئر اونرشپ ماڈل۔",
    "EF Core Migration": "EF Core مائیگریشن",
    "Database blueprint updated for S23-S36 entities and workflows.": "S23-S36 اینٹٹیز اور ورک فلوز کے لیے ڈیٹابیس بلیوپرنٹ اپڈیٹ ہو چکا ہے۔",
    "Patient Operations": "پیشنٹ آپریشنز",
    "Patient Registry And Health Record Workspace": "پیشنٹ رجسٹری اور ہیلتھ ریکارڈ ورک اسپیس",
    "Search Registry": "رجسٹری تلاش کریں",
    "Reload Patient": "مریض دوبارہ لوڈ کریں",
    "Register Patient": "مریض رجسٹر کریں",
    "Selected Patient": "منتخب مریض",
    "Vitals Entries": "وائیٹل اندراجات",
    "tracked history": "محفوظ شدہ ہسٹری",
    "Vital Trends": "وائیٹل رجحانات",
    "live indicators": "لائیو اشاریے",
    "Insurance": "انشورنس",
    "Self Pay": "خود ادائیگی",
    "No policy captured": "کوئی پالیسی محفوظ نہیں",
    "Live patient context": "لائیو مریضی سیاق",
    "Care Coordination": "کیئر کوآرڈینیشن",
    "City pending": "شہر درج نہیں",
    "Premium shortcuts": "پریمیم شارٹ کٹس",
    "Next Actions": "اگلے اقدامات",
    "Book Appointment": "اپائنٹمنٹ بُک کریں",
    "Carry patient context into scheduling": "مریض کے سیاق کو شیڈیولنگ میں لے جائیں",
    "Open Timeline": "ٹائم لائن کھولیں",
    "Review appointments, vitals, labs, and pharmacy activity": "اپائنٹمنٹس، وائیٹلز، لیبز، اور فارمیسی سرگرمی کا جائزہ لیں",
    "Patient Portal": "پیشنٹ پورٹل",
    "Validate dashboard and appointment history with the same patient id": "اسی مریضی شناخت کے ساتھ ڈیش بورڈ اور اپائنٹمنٹ ہسٹری دیکھیں",
    "Doctor Discovery": "ڈاکٹر ڈسکوری",
    "Launch recommendation engine for this patient journey": "اس مریضی سفر کے لیے ریکمینڈیشن انجن چلائیں",
    "Step 1": "مرحلہ 1",
    "Register And Search": "رجسٹر اور تلاش کریں",
    "Registry intake": "رجسٹری ان ٹیک",
    "Search existing patients or onboard a new patient record with complete contact and emergency context.": "موجودہ مریض تلاش کریں یا مکمل رابطہ اور ایمرجنسی سیاق کے ساتھ نیا مریضی ریکارڈ آن بورڈ کریں۔",
    "Default password policy is preloaded and hidden for safer operator review.": "محفوظ آپریٹر ریویو کے لیے ڈیفالٹ پاس ورڈ پالیسی پہلے سے لوڈ اور پوشیدہ رکھی گئی ہے۔",
    "First Name": "پہلا نام",
    "Last Name": "آخری نام",
    "CNIC": "شناختی کارڈ",
    "Date Of Birth": "تاریخ پیدائش",
    "Gender": "جنس",
    "Female": "خاتون",
    "Male": "مرد",
    "Other": "دیگر",
    "Blood Group": "بلڈ گروپ",
    "Phone": "فون",
    "City": "شہر",
    "Emergency Contact": "ایمرجنسی رابطہ",
    "Emergency Phone": "ایمرجنسی فون",
    "Search": "تلاش",
    "Reset Form": "فارم ری سیٹ کریں",
    "No patients found yet. Search the registry or register a new patient to start the care flow.": "ابھی کوئی مریض نہیں ملا۔ کیئر فلو شروع کرنے کے لیے رجسٹری تلاش کریں یا نیا مریض رجسٹر کریں۔",
    "Contact": "رابطہ",
    "Emergency": "ایمرجنسی",
    "Not captured": "محفوظ نہیں",
    "Step 2": "مرحلہ 2",
    "Profile And Coverage": "پروفائل اور کوریج",
    "Search or register a patient to activate enterprise profile management.": "انٹرپرائز پروفائل مینجمنٹ فعال کرنے کے لیے مریض تلاش کریں یا رجسٹر کریں۔",
    "No phone": "فون موجود نہیں",
    "No city": "شہر موجود نہیں",
    "Self pay": "خود ادائیگی",
    "Alternate Phone": "متبادل فون",
    "Street": "گلی",
    "Province": "صوبہ",
    "Postal Code": "پوسٹل کوڈ",
    "Emergency Relation": "ایمرجنسی رشتہ",
    "Insurance Provider": "انشورنس فراہم کنندہ",
    "Insurance Policy": "انشورنس پالیسی",
    "Save Profile": "پروفائل محفوظ کریں",
    "Step 3": "مرحلہ 3",
    "Medical History": "میڈیکل ہسٹری",
    "Clinical Context": "کلینیکل سیاق",
    "Patient medical history becomes editable after a patient is selected.": "کسی مریض کے منتخب ہونے کے بعد میڈیکل ہسٹری قابلِ ترمیم ہو جاتی ہے۔",
    "Doctor Operations": "ڈاکٹر آپریشنز",
    "Doctor Discovery, Credential, And Availability Workspace": "ڈاکٹر ڈسکوری، کریڈنشل، اور دستیابی ورک اسپیس",
    "Refresh Search": "تلاش ریفریش کریں",
    "Load Slots": "سلاٹس لوڈ کریں",
    "Save Doctor": "ڈاکٹر محفوظ کریں",
    "Selected Doctor": "منتخب ڈاکٹر",
    "Rating": "درجہ بندی",
    "reviews": "ریویوز",
    "editable slots": "قابلِ ترمیم سلاٹس",
    "Available Slots": "دستیاب سلاٹس",
    "Operational context": "آپریشنل سیاق",
    "Doctor Snapshot": "ڈاکٹر اسنیپ شاٹ",
    "PMDC": "PMDC",
    "Schedule Appointment": "اپائنٹمنٹ شیڈیول کریں",
    "Carry doctor context straight into booking orchestration": "ڈاکٹر کا سیاق براہِ راست بُکنگ ورک فلو میں لے جائیں",
    "Doctor Portal": "ڈاکٹر پورٹل",
    "Open queue, schedule, and history from the same doctor context": "اسی ڈاکٹر سیاق سے کیو، شیڈول، اور ہسٹری کھولیں",
    "Recommendation View": "ریکمینڈیشن ویو",
    "Preview how discovery ranks this doctor by city and specialization": "دیکھیں کہ ڈسکوری اس ڈاکٹر کو شہر اور اسپیشلائزیشن کے مطابق کیسے رینک کرتی ہے",
    "Admin Oversight": "ایڈمن نگرانی",
    "Escalate to credential and performance governance": "کریڈنشل اور پرفارمنس گورننس کے لیے آگے بڑھائیں",
    "Discovery Search": "ڈسکوری تلاش",
    "Search by operational fit": "آپریشنل فٹ کے مطابق تلاش",
    "Filter doctors by specialization, city, and fee to shortlist the right clinician for the patient journey.": "مریضی سفر کے لیے درست کلینیشن منتخب کرنے کو اسپیشلائزیشن، شہر، اور فیس کے مطابق ڈاکٹروں کو فلٹر کریں۔",
    "Specialization": "اسپیشلائزیشن",
    "Max Fee": "زیادہ سے زیادہ فیس",
    "Cardiology, Neurology, Dermatology": "کارڈیالوجی، نیورولوجی، ڈرماٹولوجی",
    "Karachi, Lahore, Islamabad": "کراچی، لاہور، اسلام آباد",
    "No doctors matched the current filters. Adjust search criteria to expand the shortlist.": "موجودہ فلٹرز کے مطابق کوئی ڈاکٹر نہیں ملا۔ فہرست بڑھانے کے لیے معیار تبدیل کریں۔",
    "Location": "مقام",
    "Fee / Rating": "فیس / ریٹنگ",
    "Doctor Profile": "ڈاکٹر پروفائل",
    "Search and select a doctor to unlock profile, verification, and schedule tools.": "پروفائل، ویریفکیشن، اور شیڈول ٹولز کھولنے کے لیے ڈاکٹر تلاش اور منتخب کریں۔",
    "Qualification": "اہلیت",
    "Consultation Fee": "مشاورت فیس",
    "Biography": "تعارف",
    "Active Profile": "فعال پروفائل",
    "Verified": "تصدیق شدہ",
    "Update Verification": "ویریفکیشن اپڈیٹ کریں",
    "Schedule Studio": "شیڈول اسٹوڈیو",
    "slots": "سلاٹس",
    "Select a doctor to edit recurring weekly schedule slots.": "بار بار آنے والے ہفتہ وار شیڈول سلاٹس ایڈٹ کرنے کے لیے ڈاکٹر منتخب کریں۔",
    "Recurring slot design": "ریکرنگ سلاٹ ڈیزائن",
    "Use weekly slots to define clinic rhythm and online coverage before opening booking inventory.": "بُکنگ انوینٹری کھولنے سے پہلے کلینک کا ہفتہ وار رِدھم اور آن لائن کوریج طے کریں۔",
    "Day": "دن",
    "Start": "آغاز",
    "End": "اختتام",
    "Appointment Operations": "اپائنٹمنٹ آپریشنز",
    "Appointment Booking And Lifecycle Workspace": "اپائنٹمنٹ بُکنگ اور لائف سائیکل ورک اسپیس",
    "Search Appointments": "اپائنٹمنٹس تلاش کریں",
    "Reload Detail": "تفصیل دوبارہ لوڈ کریں",
    "Appointment": "اپائنٹمنٹ",
    "fee": "فیس",
    "Queue / Payment": "کیو / ادائیگی",
    "No queue": "کوئی کیو نہیں",
    "Guided intake": "گائیڈڈ ان ٹیک",
    "Booking Studio": "بُکنگ اسٹوڈیو",
    "Step 1A": "مرحلہ 1A",
    "Patient Context": "مریضی سیاق",
    "Patient Search": "مریض تلاش",
    "Name, email, CNIC, or phone": "نام، ای میل، شناختی کارڈ، یا فون",
    "Find Patients": "مریض تلاش کریں",
    "Open Registry": "رجسٹری کھولیں",
    "Search patients here instead of manually copying ids into booking.": "شناختی نمبرز دستی طور پر کاپی کرنے کے بجائے مریض یہیں تلاش کریں۔",
    "Use": "استعمال کریں",
    "Step 1B": "مرحلہ 1B",
    "Doctor Context": "ڈاکٹری سیاق",
    "Cardiology, Neurology, Pediatrics": "کارڈیالوجی، نیورولوجی، پیڈیاٹرکس",
    "Find Doctors": "ڈاکٹر تلاش کریں",
    "Open Doctors": "ڈاکٹرز کھولیں",
    "Search doctors here to build appointment context without manual id lookups.": "دستی شناختی تلاش کے بغیر اپائنٹمنٹ سیاق بنانے کے لیے ڈاکٹر یہیں تلاش کریں۔",
    "Booking Details": "بُکنگ تفصیلات",
    "Ready to book": "بُکنگ کے لیے تیار",
    "Patient Id": "مریض شناخت",
    "Doctor Id": "ڈاکٹر شناخت",
    "Scheduled At": "شیڈیول وقت",
    "Type": "قسم",
    "Duration Minutes": "مدت منٹس",
    "Priority": "ترجیح",
    "Reason For Visit": "وزٹ کی وجہ",
    "Patient Notes": "مریض نوٹس",
    "Use As Search Filter": "تلاش فلٹر کے طور پر استعمال کریں",
    "Reset Booking": "بُکنگ ری سیٹ کریں",
    "Clear Context": "سیاق صاف کریں",
    "Operational search": "آپریشنل تلاش",
    "Search And Timeline": "تلاش اور ٹائم لائن",
    "Track lifecycle across the day": "دن بھر میں لائف سائیکل ٹریک کریں",
    "Use patient, doctor, status, and date filters to audit active and historical appointment activity.": "فعال اور تاریخی اپائنٹمنٹ سرگرمی جانچنے کے لیے مریض، ڈاکٹر، اسٹیٹس، اور تاریخ کے فلٹرز استعمال کریں۔",
    "Date": "تاریخ",
    "Use Booking Context": "بُکنگ سیاق استعمال کریں",
    "No appointments matched the active filters.": "فعال فلٹرز کے مطابق کوئی اپائنٹمنٹ نہیں ملی۔",
    "S31 Timeline": "S31 ٹائم لائن",
    "Patient Health Timeline": "مریض ہیلتھ ٹائم لائن",
    "Summary PDF": "خلاصہ PDF",
    "Emergency QR": "ایمرجنسی QR",
    "Filter": "فلٹر",
    "All": "تمام",
    "Vitals": "وائیٹلز",
    "Lab": "لیب",
    "Dispense": "ڈسپنس",
    "Entries": "اندراجات",
    "chronological": "تاریخی ترتیب",
    "No timeline entries are available for the selected patient and filter yet.": "منتخب مریض اور فلٹر کے لیے ابھی کوئی ٹائم لائن اندراج دستیاب نہیں۔",
    "Enter a valid patient id.": "درست مریض شناخت درج کریں۔",
    "Health timeline loaded.": "ہیلتھ ٹائم لائن لوڈ ہو گئی۔",
    "Timeline unavailable.": "ٹائم لائن دستیاب نہیں۔",
    "Health summary download failed.": "ہیلتھ خلاصہ ڈاؤن لوڈ ناکام رہا۔",
    "Health summary downloaded.": "ہیلتھ خلاصہ ڈاؤن لوڈ ہو گیا۔",
    "Emergency card download failed.": "ایمرجنسی کارڈ ڈاؤن لوڈ ناکام رہا۔",
    "Emergency card downloaded.": "ایمرجنسی کارڈ ڈاؤن لوڈ ہو گیا۔",
    "Portal Filters": "پورٹل فلٹرز",
    "AppointmentId": "اپائنٹمنٹ شناخت",
    "Apply": "لاگو کریں",
    "Appointments": "اپائنٹمنٹس",
    "Detail": "تفصیل",
    "Upcoming": "آنے والی",
    "appointments": "اپائنٹمنٹس",
    "Past": "گزشتہ",
    "records": "ریکارڈز",
    "Next": "اگلا",
    "visit": "وزٹ",
    "Actions": "اقدامات",
    "available": "دستیاب",
    "No upcoming appointments are available for this patient context.": "اس مریضی سیاق کے لیے کوئی آئندہ اپائنٹمنٹ دستیاب نہیں۔",
    "No completed or historical appointments are available yet.": "ابھی کوئی مکمل یا تاریخی اپائنٹمنٹ دستیاب نہیں۔",
    "Quick Actions": "فوری اقدامات",
    "No quick actions are available for this patient at the moment.": "اس وقت اس مریض کے لیے کوئی فوری اقدام دستیاب نہیں۔",
    "Action": "اقدام",
    "View": "دیکھیں",
    "Doctor Context": "ڈاکٹری سیاق",
    "History": "ہسٹری",
    "waiting": "انتظار میں",
    "done": "مکمل",
    "today": "آج",
    "Today's Queue": "آج کی کیو",
    "active": "فعال",
    "No appointments are queued for the selected doctor and date yet.": "منتخب ڈاکٹر اور تاریخ کے لیے ابھی کوئی اپائنٹمنٹ کیو میں نہیں۔",
    "Token": "ٹوکن",
    "No upcoming appointments are scheduled after the current queue window.": "موجودہ کیو ونڈو کے بعد کوئی آئندہ اپائنٹمنٹ شیڈیول نہیں۔",
    "My Schedule": "میرا شیڈول",
    "No recurring schedule has been configured for this doctor yet.": "اس ڈاکٹر کے لیے ابھی کوئی ریکرنگ شیڈول ترتیب نہیں دیا گیا۔",
    "Window": "ونڈو",
    "Slot": "سلاٹ",
    "Mode": "موڈ",
    "Allergies": "الرجیز",
    "Chronic": "دائمی امراض",
    "consults": "کنسلٹس",
    "S33 Recommendations": "S33 سفارشات",
    "Best Match Doctor Discovery": "بہترین میچ ڈاکٹر ڈسکوری",
    "Find Best Match": "بہترین میچ تلاش کریں",
    "Appointment Type": "اپائنٹمنٹ قسم",
    "Recommendations": "سفارشات",
    "No recommendation set loaded yet. Provide patient or specialty context to generate best-match doctors.": "ابھی سفارشاتی فہرست لوڈ نہیں ہوئی۔ بہترین میچ ڈاکٹرز بنانے کے لیے مریض یا اسپیشلٹی سیاق دیں۔",
    "Doctor recommendations loaded.": "ڈاکٹر سفارشات لوڈ ہو گئیں۔",
    "Doctor recommendations unavailable.": "ڈاکٹر سفارشات دستیاب نہیں۔",
    "Gateway Checkout & Refunds": "گیٹ وے چیک آؤٹ اور ریفنڈز",
    "Home Collection, Results & Report Release": "ہوم کلیکشن، نتائج، اور رپورٹ ریلیز",
    "Global Analytics Dashboard": "گلوبل اینالیٹکس ڈیش بورڈ",
    "Sessions, Login History & 2FA": "سیشنز، لاگ اِن ہسٹری، اور 2FA",
    "Navigation Studio": "نیویگیشن اسٹوڈیو",
    "Home Collection Ops": "ہوم کلیکشن آپریشنز",
    "Tenant Id": "ٹیننٹ شناخت",
    "Two-Factor Setup": "ٹو فیکٹر سیٹ اپ",
    "User Menu Assignment": "یوزر مینو اسائنمنٹ",
    "Ready": "تیار",
    "Operational": "آپریشنل",
    "Online": "آن لائن",
    "OnSite": "آن سائٹ",
    "Pending": "زیر التوا",
    "Confirmed": "تصدیق شدہ",
    "InProgress": "جاری",
    "Cancelled": "منسوخ",
    "NoShow": "غیر حاضر",
    "Normal": "عام",
    "High": "اہم",
    "Urgent": "فوری"
  });

  function normalizeCulture(value) {
    return value === rtlCulture ? rtlCulture : "en";
  }

  function normalizeText(value) {
    return (value ?? "").replace(/\s+/g, " ").trim();
  }

  function getTranslation(value, culture) {
    if (culture !== rtlCulture) {
      return value;
    }

    const normalized = normalizeText(value);
    if (!normalized) {
      return value;
    }

    const translated = translationsUr[normalized];
    if (!translated) {
      return value;
    }

    const leading = (value.match(/^\s*/) || [""])[0];
    const trailing = (value.match(/\s*$/) || [""])[0];
    return `${leading}${translated}${trailing}`;
  }

  function rememberTextNode(node, value) {
    originalText.set(node, value ?? "");
  }

  function rememberAttribute(element, attribute) {
    let elementAttributes = originalAttributes.get(element);
    if (!elementAttributes) {
      elementAttributes = new Map();
      originalAttributes.set(element, elementAttributes);
    }

    elementAttributes.set(attribute, element.getAttribute(attribute) ?? "");
  }

  function applyTextNode(node, culture) {
    if (!originalText.has(node)) {
      rememberTextNode(node, node.nodeValue);
    }

    const source = originalText.get(node) ?? "";
    node.nodeValue = culture === rtlCulture ? getTranslation(source, culture) : source;
  }

  function shouldTranslateNode(node) {
    const parent = node.parentElement;
    if (!parent) {
      return false;
    }

    if (["SCRIPT", "STYLE", "TEXTAREA", "INPUT"].includes(parent.tagName)) {
      return false;
    }

    return !!normalizeText(node.nodeValue);
  }

  function applyAttributes(root, culture) {
    const elements = root instanceof Element ? [root, ...root.querySelectorAll("*")] : [];
    elements.forEach((element) => {
      translatableAttributes.forEach((attribute) => {
        if (!element.hasAttribute(attribute)) {
          return;
        }

        if (!originalAttributes.has(element) || !originalAttributes.get(element).has(attribute)) {
          rememberAttribute(element, attribute);
        }

        const source = originalAttributes.get(element).get(attribute) ?? "";
        element.setAttribute(attribute, culture === rtlCulture ? getTranslation(source, culture) : source);
      });
    });
  }

  function applyTranslations(root, culture) {
    if (!root) {
      return;
    }

    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, {
      acceptNode: (node) => shouldTranslateNode(node) ? NodeFilter.FILTER_ACCEPT : NodeFilter.FILTER_REJECT
    });

    let current = walker.nextNode();
    while (current) {
      applyTextNode(current, culture);
      current = walker.nextNode();
    }

    applyAttributes(root, culture);
  }

  function runApplying(action) {
    isApplying = true;
    try {
      action();
    } finally {
      isApplying = false;
    }
  }

  function setDocumentCulture(culture) {
    const normalized = normalizeCulture(culture);
    document.documentElement.lang = normalized;
    document.documentElement.dir = normalized === rtlCulture ? "rtl" : "ltr";
    document.documentElement.setAttribute("data-culture", normalized);
    if (document.body) {
      document.body.setAttribute("data-culture", normalized);
    }

    localStorage.setItem(key, normalized);
    return normalized;
  }

  function currentCulture() {
    return normalizeCulture(localStorage.getItem(key));
  }

  function ensureObserver() {
    if (observerStarted || !document.documentElement) {
      return;
    }

    observerStarted = true;
    const observer = new MutationObserver((mutations) => {
      if (isApplying) {
        return;
      }

      const culture = currentCulture();
      mutations.forEach((mutation) => {
        if (mutation.type === "characterData" && mutation.target.nodeType === Node.TEXT_NODE) {
          rememberTextNode(mutation.target, mutation.target.nodeValue);
          runApplying(() => applyTextNode(mutation.target, culture));
          return;
        }

        if (mutation.type === "attributes" && mutation.target instanceof Element && mutation.attributeName) {
          rememberAttribute(mutation.target, mutation.attributeName);
          runApplying(() => applyAttributes(mutation.target, culture));
          return;
        }

        if (mutation.type === "childList") {
          mutation.addedNodes.forEach((node) => {
            if (node.nodeType === Node.TEXT_NODE) {
              rememberTextNode(node, node.nodeValue);
              runApplying(() => applyTextNode(node, culture));
              return;
            }

            if (node.nodeType === Node.ELEMENT_NODE) {
              runApplying(() => applyTranslations(node, culture));
            }
          });
        }
      });
    });

    observer.observe(document.documentElement, {
      subtree: true,
      childList: true,
      characterData: true,
      attributes: true,
      attributeFilter: translatableAttributes
    });
  }

  function apply(culture) {
    const normalized = setDocumentCulture(culture);
    runApplying(() => applyTranslations(document.documentElement, normalized));
    ensureObserver();
    return normalized;
  }

  function init() {
    return apply(currentCulture());
  }

  return {
    init: init,
    apply: apply,
    getCurrentCulture: currentCulture
  };
})();
