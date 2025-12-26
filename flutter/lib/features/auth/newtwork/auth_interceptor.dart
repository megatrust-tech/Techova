import 'package:dio/dio.dart';
import '../../../core/constants/api_endpoints.dart';
import '../../../core/network/dio_client.dart';
import '../services/token_storage_service.dart';

class AuthInterceptor extends Interceptor {
  final TokenStorageService _storageService;
  final DioClient _dioClient; // Use DioClient for retries

  // Flag to prevent infinite refresh loops
  bool _isRefreshing = false;

  AuthInterceptor(this._storageService, this._dioClient);

  @override
  Future<void> onRequest(
      RequestOptions options, RequestInterceptorHandler handler) async {

    // 1. Skip adding token for public endpoints (Login/Refresh)
    if (options.path.contains(ApiEndpoints.login) ||
        options.path.contains(ApiEndpoints.refreshToken)) {
      return handler.next(options);
    }

    // 2. Get token from storage
    final accessToken = await _storageService.getAccessToken();

    // 3. Attach to header if exists
    if (accessToken != null && accessToken.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $accessToken';
    }

    return handler.next(options);
  }

  @override
  Future<void> onError(
      DioException err, ErrorInterceptorHandler handler) async {

    // 1. Check if error is 401 Unauthorized and we haven't tried refreshing yet
    if (err.response?.statusCode == 401 && !_isRefreshing) {

      // Ensure we don't refresh if the failed request WAS a refresh attempt
      if (err.requestOptions.path.contains(ApiEndpoints.refreshToken)) {
        await _storageService.clearTokens();
        return handler.next(err);
      }

      _isRefreshing = true;

      try {
        // 2. Get the refresh token
        final refreshToken = await _storageService.getRefreshToken();

        if (refreshToken == null) {
          // No refresh token, force logout flow
          _isRefreshing = false;
          return handler.next(err);
        }

        // 3. Perform refresh using a fresh Dio instance to avoid triggering interceptors recursively
        final refreshDio = Dio(BaseOptions(baseUrl: ApiEndpoints.baseUrl));

        final response = await refreshDio.post(
          ApiEndpoints.refreshToken,
          data: {'refreshToken': refreshToken},
        );

        if (response.statusCode == 200) {
          // 4. Parse and Save new tokens
          final newAccessToken = response.data['accessToken'] ?? response.data['AccessToken'];
          final newRefreshToken = response.data['refreshToken'] ?? response.data['RefreshToken'];

          if (newAccessToken != null) {
            await _storageService.saveTokens(
              accessToken: newAccessToken,
              refreshToken: newRefreshToken ?? refreshToken,
            );

            // 5. Retry the original request with the new token
            final opts = err.requestOptions;
            opts.headers['Authorization'] = 'Bearer $newAccessToken';

            final clonedRequest = await _dioClient.dio.request(
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

            _isRefreshing = false;
            return handler.resolve(clonedRequest);
          }
        }
      } catch (e) {
        // Refresh failed, user must login again
        await _storageService.clearTokens();
      } finally {
        _isRefreshing = false;
      }
    }

    return handler.next(err);
  }
}