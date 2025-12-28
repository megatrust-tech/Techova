part of 'auth_bloc.dart';

/// Base class for all authentication states
sealed class AuthState {
  const AuthState();
}

/// Initial state before any authentication check
final class AuthInitial extends AuthState {
  const AuthInitial();
}

/// Loading state during app startup (checking stored token)
final class AuthLoading extends AuthState {
  const AuthLoading();
}

/// Loading state during login - stays on login screen
final class LoginLoading extends AuthState {
  const LoginLoading();
}

class LogoutLoading extends AuthState {
  const LogoutLoading();
}


/// User is authenticated successfully
/// Holds the token response with access token and user role info
final class Authenticated extends AuthState {
  final TokenResponse tokenResponse;

  const Authenticated(this.tokenResponse);
}

/// User is not authenticated (no valid token)
final class Unauthenticated extends AuthState {
  const Unauthenticated();
}

/// Authentication failed with an error
final class AuthFailure extends AuthState {
  final String message;

  const AuthFailure(this.message);
}
