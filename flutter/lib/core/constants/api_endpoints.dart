class ApiEndpoints {
  // Base Configuration
  static const String baseUrl = 'https://taskedin-be.s-tech.digital';
  // static const String baseUrl = 'https://df59c3f6a8eb.ngrok-free.app';
  static const Duration connectionTimeout = Duration(seconds: 15);
  static const Duration receiveTimeout = Duration(seconds: 15);

  // Auth
  static const String login = '/v1/auth/login';
  static const String refreshToken = '/v1/auth/refresh';
  static const String logout = '/v1/auth/logout';
  static const String userInfo = '/v1/auth/user-info';
  static const String adminOnly = '/v1/auth/admin-only';

  // Roles
  static const String roles = '/v1/roles';

  // Users
  static const String users = '/v1/users';
  static const String userDevices = '/v1/users/devices';
  static String userById(int id) => '/v1/users/$id';


  // Notifications
  static const String notifications = '/v1/notifications';
  static const String notificationUnreadCount = '/v1/notifications/unread-count';
  static const String notificationReadAll = '/v1/notifications/read-all';

  // Helper for dynamic paths
  static String notificationRead(int id) => '/v1/notifications/$id/read';

  // Leaves - General
  static const String leavesHealth = '/leaves/health';
  static const String leaveTypes = '/leaves/leave-types';
  static const String leaveStatuses = '/leaves/leave-statuses';

  // Leaves - Employee Actions
  static const String leaves = '/leaves';
  static const String myLeaves = '/leaves/my-leaves';
  static const String remainingLeaves = '/leaves/remaining-leaves';
  static const String checkConflict = '/leaves/check-conflict';

  // Leaves - Dynamic Paths
  static String cancelLeave(int id) => '/leaves/$id/cancel';
  static String leaveHistory(int id) => '/leaves/$id/history';

  // Leaves - Management Actions
  static const String pendingApprovals = '/leaves/pending-approval';
  static const String pendingApprovalCount = '/leaves/pending-approval-count';
  static const String departmentCoverage = '/leaves/coverage';
  static String managerAction(int id) => '/leaves/$id/manager-action';
  static String hrAction(int id) => '/leaves/$id/hr-action';

  // Leaves - Admin Settings
  static const String leaveSettings = '/leaves/settings';

  // Calendar
  static const String calendarData = '/leaves/calendar';

  // Audit Logs
  static const String auditLogsDownload = '/leaves/audit-logs/download';
}