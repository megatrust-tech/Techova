part of 'auth_bloc.dart';

/// Base class for all authentication events
sealed class AuthEvent {
  const AuthEvent();
}

/// Dispatched on app startup to check if user is authenticated
/// Checks for stored tokens in secure storage
final class AppStarted extends AuthEvent {
  const AppStarted();
}

/// Dispatched when user submits login credentials
final class LoginRequested extends AuthEvent {
  final String email;
  final String password;

  const LoginRequested({
    required this.email,
    required this.password,
  });
}

/// Dispatched when user requests to logout
final class LogoutRequested extends AuthEvent {
  const LogoutRequested();
}
