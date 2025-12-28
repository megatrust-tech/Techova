import 'dart:async';

import 'package:dio/dio.dart';
import '../../../core/constants/api_endpoints.dart';
import '../../../core/network/dio_client.dart';
import '../services/auth_session_manager.dart';
import '../services/token_storage_service.dart';

class AuthInterceptor extends Interceptor {
  final TokenStorageService _storageService;
  final DioClient _dioClient;

  // Flag to prevent infinite refresh loops
  bool _isRefreshing = false;
  
  // Completer to queue requests while refreshing
  Completer<String?>? _refreshCompleter;

  AuthInterceptor(this._storageService, this._dioClient);

  /// Check if path is a public endpoint (no auth token needed)
  bool _isPublicEndpoint(String path) {
    return path.contains(ApiEndpoints.login) ||
           path.contains(ApiEndpoints.refreshToken);
  }

  /// Check if path should skip 401 handling (shouldn't trigger force logout)
  /// - Public endpoints: 401 means invalid credentials, not expired token
  /// - Device endpoints: 401 during logout shouldn't cause infinite loop
  bool _shouldSkip401Handling(String path) {
    return _isPublicEndpoint(path) ||
           path.contains(ApiEndpoints.userDevices) ||
           path.contains('/users/devices');
  }

  @override
  Future<void> onRequest(
      RequestOptions options, RequestInterceptorHandler handler) async {

    // Skip adding token for public endpoints only (Login/Refresh)
    if (_isPublicEndpoint(options.path)) {
      return handler.next(options);
    }

    // Get token from storage and attach to header
    final accessToken = await _storageService.getAccessToken();
    if (accessToken != null && accessToken.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $accessToken';
    }


    return handler.next(options);
  }

  @override
  Future<void> onError(
      DioException err, ErrorInterceptorHandler handler) async {

    print('[AuthInterceptor] onError: ${err.response?.statusCode} for ${err.requestOptions.path}');

    // IMPORTANT: Skip 401 handling for login/refresh endpoints!
    // A 401 on login means invalid credentials, NOT expired token
    if (_shouldSkip401Handling(err.requestOptions.path)) {
      print('[AuthInterceptor] Skipping - public endpoint');
      return handler.next(err);
    }

    // Only handle 401 Unauthorized
    if (err.response?.statusCode != 401) {
      return handler.next(err);
    }

    print('[AuthInterceptor] Got 401, isRefreshing: $_isRefreshing');

    // If already refreshing, wait for the refresh to complete
    if (_isRefreshing) {
      print('[AuthInterceptor] Already refreshing, waiting...');
      try {
        final newToken = await _refreshCompleter?.future;
        if (newToken != null) {
          print('[AuthInterceptor] Got new token from queue, retrying');
          final response = await _retryRequest(err.requestOptions, newToken);
          return handler.resolve(response);
        }
      } catch (_) {
        print('[AuthInterceptor] Queue wait failed');
      }
      return handler.next(err);
    }

    // Start refresh process
    _isRefreshing = true;
    _refreshCompleter = Completer<String?>();

    try {
      final refreshToken = await _storageService.getRefreshToken();
      print('[AuthInterceptor] Refresh token exists: ${refreshToken != null}');

      if (refreshToken == null) {
        print('[AuthInterceptor] No refresh token - forcing logout');
        _completeRefresh(null);
        await _forceLogout();
        return handler.next(err);
      }

      // Use a fresh Dio instance to avoid triggering interceptors recursively
      final refreshDio = Dio(BaseOptions(baseUrl: ApiEndpoints.baseUrl));

      print('[AuthInterceptor] Calling refresh endpoint...');
      final response = await refreshDio.post(
        ApiEndpoints.refreshToken,
        data: {'refreshToken': refreshToken},
      );

      print('[AuthInterceptor] Refresh response: ${response.statusCode}');

      if (response.statusCode == 200) {
        final newAccessToken = response.data['accessToken'] ?? response.data['AccessToken'];
        final newRefreshToken = response.data['refreshToken'] ?? response.data['RefreshToken'];

        if (newAccessToken != null) {
          print('[AuthInterceptor] Got new access token, saving...');
          await _storageService.saveTokens(
            accessToken: newAccessToken,
            refreshToken: newRefreshToken ?? refreshToken,
          );

          _completeRefresh(newAccessToken);

          final retryResponse = await _retryRequest(err.requestOptions, newAccessToken);
          return handler.resolve(retryResponse);
        }
      }

      print('[AuthInterceptor] Refresh failed - no valid token returned');
      _completeRefresh(null);
      await _forceLogout();
    } catch (e) {
      print('[AuthInterceptor] Refresh exception: $e');
      _completeRefresh(null);
      await _forceLogout();
    }

    return handler.next(err);
  }

  void _completeRefresh(String? token) {
    _isRefreshing = false;
    if (_refreshCompleter != null && !_refreshCompleter!.isCompleted) {
      _refreshCompleter!.complete(token);
    }
    _refreshCompleter = null;
  }

  Future<Response> _retryRequest(RequestOptions opts, String newToken) async {
    opts.headers['Authorization'] = 'Bearer $newToken';
    return await _dioClient.dio.request(
      opts.path,
      options: Options(
        method: opts.method,
        headers: opts.headers,
        responseType: opts.responseType,
        contentType: opts.contentType,
      ),
      data: opts.data,
      queryParameters: opts.queryParameters,
    );
  }

  Future<void> _forceLogout() async {
    print('[AuthInterceptor] !!! FORCE LOGOUT TRIGGERED !!!');
    await _storageService.clearTokens();
    AuthSessionManager.instance.triggerForceLogout();
  }
}