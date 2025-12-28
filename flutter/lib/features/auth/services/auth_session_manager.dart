import 'dart:async';

/// Singleton service to manage auth session events across the app
/// 
/// This allows the AuthInterceptor to signal a force logout
/// without directly depending on AuthBloc
class AuthSessionManager {
  AuthSessionManager._();
  static final AuthSessionManager instance = AuthSessionManager._();

  final _forceLogoutController = StreamController<void>.broadcast();

  /// Stream that emits when a force logout is needed (e.g., token refresh failed)
  Stream<void> get forceLogoutStream => _forceLogoutController.stream;

  /// Call this when token refresh fails and user must be logged out
  void triggerForceLogout() {
    _forceLogoutController.add(null);
  }

  /// Clean up resources (call on app dispose if needed)
  void dispose() {
    _forceLogoutController.close();
  }
}
