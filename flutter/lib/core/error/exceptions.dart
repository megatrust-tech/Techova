class ServerException implements Exception {
  final String? message;
  final int? statusCode;

  ServerException({this.message, this.statusCode});
}

class CacheException implements Exception {
  final String message;

  CacheException({this.message = "Cache Failure"});
}

class AuthException implements Exception {
  final String message;

  AuthException({this.message = "Authentication Failure"});
}