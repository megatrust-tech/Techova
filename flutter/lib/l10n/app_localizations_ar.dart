// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Arabic (`ar`).
class AppLocalizationsAr extends AppLocalizations {
  AppLocalizationsAr([String locale = 'ar']) : super(locale);

  @override
  String get appTitle => 'تاسكد إن';

  @override
  String get welcomeBack => 'مرحباً بعودتك،';

  @override
  String get loading => 'جاري التحميل...';

  @override
  String get home => 'الرئيسية';

  @override
  String get leaves => 'الإجازات';

  @override
  String get calendar => 'التقويم';

  @override
  String get profile => 'الملف الشخصي';

  @override
  String get login => 'تسجيل الدخول';

  @override
  String get signIn => 'تسجيل الدخول';

  @override
  String get email => 'البريد الإلكتروني';

  @override
  String get password => 'كلمة المرور';

  @override
  String get pleaseEnterEmail => 'الرجاء إدخال البريد الإلكتروني';

  @override
  String get pleaseEnterValidEmail => 'الرجاء إدخال بريد إلكتروني صحيح';

  @override
  String get pleaseEnterPassword => 'الرجاء إدخال كلمة المرور';

  @override
  String get passwordMinLength => 'كلمة المرور يجب أن تكون 8 أحرف على الأقل';

  @override
  String get theme => 'المظهر';

  @override
  String get darkMode => 'الوضع الداكن';

  @override
  String get lightMode => 'الوضع الفاتح';

  @override
  String get language => 'اللغة';

  @override
  String get english => 'English';

  @override
  String get arabic => 'العربية';

  @override
  String get selectLanguage => 'اختر اللغة';

  @override
  String get logout => 'تسجيل الخروج';

  @override
  String get logoutConfirmation => 'هل أنت متأكد من تسجيل الخروج؟';

  @override
  String get cancel => 'إلغاء';

  @override
  String get comingSoon => 'قريباً...';

  @override
  String get homeDashboard => 'لوحة التحكم';

  @override
  String get notifications => 'الإشعارات';

  @override
  String get myLeaves => 'إجازاتي';

  @override
  String get applyLeave => 'طلب إجازة';

  @override
  String get teamCalendar => 'تقويم الفريق';

  @override
  String get pending => 'قيد الانتظار';

  @override
  String get approved => 'مقبول';

  @override
  String get rejected => 'مرفوض';

  @override
  String get reason => 'السبب';

  @override
  String get startDate => 'تاريخ البدء';

  @override
  String get endDate => 'تاريخ الانتهاء';

  @override
  String get submit => 'إرسال';

  @override
  String languageSetTo(String language) {
    return 'تم تغيير اللغة إلى $language';
  }

  @override
  String get newLeaveRequest => 'طلب إجازة جديد';

  @override
  String get leaveType => 'نوع الإجازة';

  @override
  String get duration => 'المدة';

  @override
  String get selectDates => 'اختر التواريخ';

  @override
  String get notes => 'ملاحظات';

  @override
  String get notesHint => 'أضف أي ملاحظات إضافية...';

  @override
  String get attachment => 'المرفقات';

  @override
  String get uploadFile => 'رفع ملف';

  @override
  String get submitRequest => 'إرسال الطلب';

  @override
  String get conflictDetected => 'تم اكتشاف تعارض';

  @override
  String get noConflict => 'لا يوجد تعارض';

  @override
  String get days => 'أيام';

  @override
  String get day => 'يوم';

  @override
  String get seeAll => 'عرض الكل';

  @override
  String get leaveBalance => 'رصيد الإجازات';

  @override
  String get markAsRead => 'تحديد كمقروء';

  @override
  String get noNotificationsYet => 'لا توجد إشعارات بعد';

  @override
  String get notificationsEmptyHint => 'عند استلام إشعارات ستظهر هنا.';

  @override
  String get today => 'اليوم';

  @override
  String get tomorrow => 'غدًا';

  @override
  String get nextWeek => 'الأسبوع القادم';

  @override
  String get fullTeamAvailable => 'الفريق بالكامل متاح';

  @override
  String get leaveTypeLabel => 'النوع';

  @override
  String get leaveStartLabel => 'البداية';

  @override
  String get leaveEndLabel => 'النهاية';

  @override
  String get leaveNotesLabel => 'ملاحظات';

  @override
  String get leaveApproved => 'مقبول';

  @override
  String get leaveRejected => 'مرفوض';

  @override
  String get leavePending => 'قيد المراجعة';

  @override
  String get leaveCanceled => 'ملغي';

  @override
  String get dayMon => 'الإثنين';

  @override
  String get dayTue => 'الثلاثاء';

  @override
  String get dayWed => 'الأربعاء';

  @override
  String get dayThu => 'الخميس';

  @override
  String get dayFri => 'الجمعة';

  @override
  String get daySat => 'السبت';

  @override
  String get daySun => 'الأحد';

  @override
  String get downloadAuditLogs => 'تحميل سجلات التدقيق';

  @override
  String get auditLogsDownloaded => 'تم تحميل سجلات التدقيق بنجاح';

  @override
  String get failedToDownloadAuditLogs => 'فشل تحميل سجلات التدقيق';

  @override
  String get unauthorizedAccess => 'وصول غير مصرح به';

  @override
  String get noAuditLogsFound => 'لا توجد سجلات تدقيق';

  @override
  String get retry => 'إعادة المحاولة';
}
