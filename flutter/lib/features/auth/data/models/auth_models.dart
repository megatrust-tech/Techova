class LoginRequest {
  final String email;
  final String password;

  LoginRequest({required this.email, required this.password});

  Map<String, dynamic> toJson() {
    return {
      'email': email,
      'password': password,
    };
  }
}

class TokenResponse {
  final String accessToken;
  final String refreshToken;
  final DateTime expiresAt;
  final int roleId;
  final String roleName;

  TokenResponse({
    required this.accessToken,
    required this.refreshToken,
    required this.expiresAt,
    required this.roleId,
    required this.roleName,
  });

  factory TokenResponse.fromJson(Map<String, dynamic> json) {
    return TokenResponse(
      // Handling both camelCase (standard JSON) and PascalCase (C# default) just in case
      accessToken: json['accessToken'] ?? json['AccessToken'] ?? '',
      refreshToken: json['refreshToken'] ?? json['RefreshToken'] ?? '',
      expiresAt: DateTime.tryParse(json['expiresAt'] ?? json['ExpiresAt'] ?? '') ?? DateTime.now(),
      roleId: json['roleId'] ?? json['RoleId'] ?? 0,
      roleName: json['roleName'] ?? json['RoleName'] ?? '',
    );
  }
}