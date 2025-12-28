import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../notification/data/repositories/push_notification_service.dart';
import '../../data/models/auth_models.dart';
import '../../data/repositories/auth_repository.dart';
import '../../services/auth_session_manager.dart';
import '../../services/token_storage_service.dart';

part 'auth_event.dart';
part 'auth_state.dart';

/// Manages authentication state for the entire app
/// 
/// Responsibilities:
/// - Check stored token on app startup
/// - Handle login with credentials
/// - Handle logout and token cleanup
/// - Listen for force logout events from AuthSessionManager
class AuthBloc extends Bloc<AuthEvent, AuthState> {
  final AuthRepository _authRepository;
  final TokenStorageService _tokenStorage;
  final PushNotificationService _push;
  StreamSubscription<void>? _forceLogoutSubscription;

  AuthBloc({
    required AuthRepository authRepository,
    required TokenStorageService tokenStorage,
    required PushNotificationService push,
  })  : _authRepository = authRepository,
        _tokenStorage = tokenStorage,
        _push = push,
        super(const AuthInitial()) {
    on<AppStarted>(_onAppStarted);
    on<LoginRequested>(_onLoginRequested);
    on<LogoutRequested>(_onLogoutRequested);
    on<ForceLogout>(_onForceLogout);
    
    // Listen for force logout events from AuthSessionManager
    _forceLogoutSubscription = AuthSessionManager.instance.forceLogoutStream.listen((_) {
      add(const ForceLogout());
    });
  }

  @override
  Future<void> close() {
    _forceLogoutSubscription?.cancel();
    return super.close();
  }

  /// Check if user has a valid stored token on app startup
  Future<void> _onAppStarted(
    AppStarted event,
    Emitter<AuthState> emit,
  ) async {
    emit(const AuthLoading());

    try {
      final hasToken = await _tokenStorage.hasToken();

      if (hasToken) {
        final accessToken = await _tokenStorage.getAccessToken();
        final refreshToken = await _tokenStorage.getRefreshToken();
        final roleId = await _tokenStorage.getRoleId();
        final roleName = await _tokenStorage.getRoleName();

        if (accessToken != null && refreshToken != null) {
          emit(Authenticated(TokenResponse(
            accessToken: accessToken,
            refreshToken: refreshToken,
            expiresAt: DateTime.now().add(const Duration(hours: 1)),
            roleId: roleId ?? 0,
            roleName: roleName ?? 'Unknown',
          )));
        } else {
          emit(const Unauthenticated());
        }
      } else {
        emit(const Unauthenticated());
      }
    } catch (e) {
      emit(const Unauthenticated());
    }
  }

  /// Handle login with email and password
  Future<void> _onLoginRequested(
    LoginRequested event,
    Emitter<AuthState> emit,
  ) async {
    emit(const LoginLoading());

    try {
      final tokenResponse = await _authRepository.login(
        event.email,
        event.password,
      );

      // Save role info to SharedPreferences
      await _tokenStorage.saveRoleInfo(
        roleId: tokenResponse.roleId,
        roleName: tokenResponse.roleName,
      );

      try {
        await _push.registerDevice();
      } catch (_) {}

      emit(Authenticated(tokenResponse));
    } on DioException catch (e) {
      // Handle specific HTTP errors
      final message = _extractErrorMessage(e);
      emit(AuthFailure(message));
    } catch (e) {
      emit(AuthFailure('An unexpected error occurred: ${e.toString()}'));
    }
  }

  /// Handle logout - clears tokens and emits unauthenticated state
  Future<void> _onLogoutRequested(
      LogoutRequested event,
      Emitter<AuthState> emit,
      ) async {
    emit(const LogoutLoading());

    try {
      await _authRepository.logout();
    } catch (_) {}
    try {
      await _push.unregisterDevice();
    } catch (_) {}
    await _tokenStorage.clearTokens();

    emit(const Unauthenticated());
  }

  /// Handle force logout - when token refresh fails
  /// Tokens are already cleared by the interceptor, just emit Unauthenticated
  Future<void> _onForceLogout(
      ForceLogout event,
      Emitter<AuthState> emit,
      ) async {
    // Tokens already cleared by interceptor
    // Just unregister device and emit Unauthenticated
    try {
      await _push.unregisterDevice();
    } catch (_) {}
    
    emit(const Unauthenticated());
  }


  /// Extract user-friendly error message from DioException
  String _extractErrorMessage(DioException e) {
    if (e.response != null) {
      final statusCode = e.response!.statusCode;
      final data = e.response!.data;

      // Try to extract message from response body
      if (data is Map<String, dynamic>) {
        final message = data['message'] ?? data['Message'] ?? data['error'];
        if (message != null) return message.toString();
      }

      // Fallback to status code messages
      switch (statusCode) {
        case 400:
          return 'Invalid request. Please check your input.';
        case 401:
          return 'Invalid email or password.';
        case 403:
          return 'Access denied.';
        case 404:
          return 'Service not found.';
        case 500:
          return 'Server error. Please try again later.';
        default:
          return 'Error: $statusCode';
      }
    }

    // Network or other errors
    if (e.type == DioExceptionType.connectionTimeout ||
        e.type == DioExceptionType.receiveTimeout) {
      return 'Connection timeout. Please check your internet.';
    }

    if (e.type == DioExceptionType.connectionError) {
      return 'Unable to connect to server. Please check your internet.';
    }

    return 'Network error. Please try again.';
  }
}
