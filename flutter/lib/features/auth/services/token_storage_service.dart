import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:jwt_decoder/jwt_decoder.dart';
import 'package:shared_preferences/shared_preferences.dart';

class TokenStorageService {
  // Using secure storage for sensitive tokens
  final _secureStorage = const FlutterSecureStorage();

  static const _keyAccessToken = 'jwt_access_token';
  static const _keyRefreshToken = 'jwt_refresh_token';
  static const _keyRoleId = 'user_role_id';
  static const _keyRoleName = 'user_role_name';
  static const _keyUserId = 'user_id';

  /// Save both tokens securely
  Future<void> saveTokens({required String accessToken, required String refreshToken}) async {
    await _secureStorage.write(key: _keyAccessToken, value: accessToken);
    await _secureStorage.write(key: _keyRefreshToken, value: refreshToken);
    
    // Extract and store userId from token
    final userId = extractUserIdFromToken(accessToken);
    if (userId != null) {
      await saveUserId(userId);
    }
  }

  /// Retrieve Access Token
  Future<String?> getAccessToken() async {
    return await _secureStorage.read(key: _keyAccessToken);
  }

  /// Retrieve Refresh Token
  Future<String?> getRefreshToken() async {
    return await _secureStorage.read(key: _keyRefreshToken);
  }

  /// Clear tokens (used for logout)
  Future<void> clearTokens() async {
    await _secureStorage.delete(key: _keyAccessToken);
    await _secureStorage.delete(key: _keyRefreshToken);
    await clearUserInfo();
  }

  /// Check if user has a token (basic check for "is logged in")
  Future<bool> hasToken() async {
    final token = await getAccessToken();
    return token != null && token.isNotEmpty;
  }

  // ─────────────────────────────────────────────────────────────
  // User ID (from JWT)
  // ─────────────────────────────────────────────────────────────

  /// Extract userId from JWT token
  int? extractUserIdFromToken(String token) {
    try {
      final decodedToken = JwtDecoder.decode(token);
      final userId = decodedToken['userId'];
      if (userId != null) {
        return int.tryParse(userId.toString());
      }
      return null;
    } catch (e) {
      return null;
    }
  }

  /// Save user ID
  Future<void> saveUserId(int userId) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setInt(_keyUserId, userId);
  }

  /// Get user ID
  Future<int?> getUserId() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getInt(_keyUserId);
  }

  // ─────────────────────────────────────────────────────────────
  // Role Info (SharedPreferences - not sensitive)
  // ─────────────────────────────────────────────────────────────

  /// Save user role info
  Future<void> saveRoleInfo({required int roleId, required String roleName}) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setInt(_keyRoleId, roleId);
    await prefs.setString(_keyRoleName, roleName);
  }

  /// Get role ID
  Future<int?> getRoleId() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getInt(_keyRoleId);
  }

  /// Get role name
  Future<String?> getRoleName() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_keyRoleName);
  }

  /// Clear user info (called on logout)
  Future<void> clearUserInfo() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_keyRoleId);
    await prefs.remove(_keyRoleName);
    await prefs.remove(_keyUserId);
  }
}
