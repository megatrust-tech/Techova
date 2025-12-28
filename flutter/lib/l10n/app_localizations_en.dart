// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appTitle => 'TaskedIn';

  @override
  String get welcomeBack => 'Welcome back,';

  @override
  String get loading => 'Loading...';

  @override
  String get home => 'Home';

  @override
  String get leaves => 'Leaves';

  @override
  String get calendar => 'Calendar';

  @override
  String get profile => 'Profile';

  @override
  String get login => 'Login';

  @override
  String get signIn => 'Sign In';

  @override
  String get email => 'Email address';

  @override
  String get password => 'Password';

  @override
  String get pleaseEnterEmail => 'Please enter your email';

  @override
  String get pleaseEnterValidEmail => 'Please enter a valid email';

  @override
  String get pleaseEnterPassword => 'Please enter your password';

  @override
  String get passwordMinLength => 'Password must be at least 8 characters';

  @override
  String get theme => 'Theme';

  @override
  String get darkMode => 'Dark Mode';

  @override
  String get lightMode => 'Light Mode';

  @override
  String get language => 'Language';

  @override
  String get english => 'English';

  @override
  String get arabic => 'العربية';

  @override
  String get selectLanguage => 'Select Language';

  @override
  String get logout => 'Logout';

  @override
  String get logoutConfirmation => 'Are you sure you want to logout?';

  @override
  String get cancel => 'Cancel';

  @override
  String get comingSoon => 'Coming Soon...';

  @override
  String get homeDashboard => 'Home Dashboard';

  @override
  String get notifications => 'Notifications';

  @override
  String get myLeaves => 'My Leaves';

  @override
  String get applyLeave => 'Apply Leave';

  @override
  String get teamCalendar => 'Team Calendar';

  @override
  String get pending => 'Pending';

  @override
  String get approved => 'Approved';

  @override
  String get rejected => 'Rejected';

  @override
  String get reason => 'Reason';

  @override
  String get startDate => 'Start Date';

  @override
  String get endDate => 'End Date';

  @override
  String get submit => 'Submit';

  @override
  String languageSetTo(String language) {
    return 'Language set to $language';
  }

  @override
  String get newLeaveRequest => 'New Leave Request';

  @override
  String get leaveType => 'Leave Type';

  @override
  String get duration => 'Duration';

  @override
  String get selectDates => 'Select Dates';

  @override
  String get notes => 'Notes';

  @override
  String get notesHint => 'Add any additional notes...';

  @override
  String get attachment => 'Attachment';

  @override
  String get uploadFile => 'Upload File';

  @override
  String get submitRequest => 'Submit Request';

  @override
  String get conflictDetected => 'A conflict was detected';

  @override
  String get noConflict => 'No conflicts found';

  @override
  String get days => 'days';

  @override
  String get day => 'day';

  @override
  String get seeAll => 'See All';

  @override
  String get leaveBalance => 'Leave Balance';

  @override
  String get markAsRead => 'Mark as Read';

  @override
  String get noNotificationsYet => 'No notifications yet';

  @override
  String get notificationsEmptyHint =>
      'When you receive notifications, they will appear here.';

  @override
  String get today => 'TODAY';

  @override
  String get tomorrow => 'TOMORROW';

  @override
  String get nextWeek => 'NEXT WEEK';

  @override
  String get fullTeamAvailable => 'Full Team Available';

  @override
  String get leaveTypeLabel => 'Type';

  @override
  String get leaveStartLabel => 'Start';

  @override
  String get leaveEndLabel => 'End';

  @override
  String get leaveNotesLabel => 'Notes';

  @override
  String get leaveApproved => 'Approved';

  @override
  String get leaveRejected => 'Rejected';

  @override
  String get leavePending => 'Pending';

  @override
  String get leaveCanceled => 'Canceled';

  @override
  String get dayMon => 'Mon';

  @override
  String get dayTue => 'Tue';

  @override
  String get dayWed => 'Wed';

  @override
  String get dayThu => 'Thu';

  @override
  String get dayFri => 'Fri';

  @override
  String get daySat => 'Sat';

  @override
  String get daySun => 'Sun';

  @override
  String get downloadAuditLogs => 'Download Audit Logs';

  @override
  String get auditLogsDownloaded => 'Audit logs downloaded successfully';

  @override
  String get failedToDownloadAuditLogs => 'Failed to download audit logs';

  @override
  String get unauthorizedAccess => 'Unauthorized access';

  @override
  String get noAuditLogsFound => 'No audit logs found';

  @override
  String get retry => 'Retry';
}
