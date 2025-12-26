
import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/dio_client.dart';
import '../../services/token_storage_service.dart';
import '../models/auth_models.dart';

class AuthRepository {
  final DioClient _dioClient;
  final TokenStorageService _storageService;

  AuthRepository(this._dioClient, this._storageService);

  /// Login: Sends credentials, saves tokens on success
  Future<TokenResponse> login(String email, String password) async {
    try {
      final request = LoginRequest(email: email, password: password);
      final response = await _dioClient.post(
        ApiEndpoints.login,
        data: request.toJson(),
      );

      final tokenResponse = TokenResponse.fromJson(response.data);

      // Persist tokens immediately so they survive app restart
      await _storageService.saveTokens(
        accessToken: tokenResponse.accessToken,
        refreshToken: tokenResponse.refreshToken,
      );

      return tokenResponse;
    } catch (e) {
      rethrow;
    }
  }

  /// Logout: Calls API then clears local storage
  Future<void> logout() async {
    try {
      // Attempt server-side logout (fire and forget mostly)
      await _dioClient.post(ApiEndpoints.logout);
    } catch (_) {
      // Ignore API errors during logout (e.g., if token already expired)
    } finally {
      // Always clear local tokens
      await _storageService.clearTokens();
    }
  }

  /// Refresh Token: Uses the refresh token to get a new access token
  /// Returns the new TokenResponse or throws if failed.
  Future<TokenResponse> refreshToken(String refreshToken) async {
    try {
      final response = await _dioClient.post(
        ApiEndpoints.refreshToken,
        data: {'refreshToken': refreshToken},
      );

      final tokenResponse = TokenResponse.fromJson(response.data);

      await _storageService.saveTokens(
        accessToken: tokenResponse.accessToken,
        refreshToken: tokenResponse.refreshToken,
      );

      return tokenResponse;
    } catch (e) {
      // If refresh fails, we must clear storage to force re-login
      await _storageService.clearTokens();
      rethrow;
    }
  }
}