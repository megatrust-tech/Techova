import 'package:dio/dio.dart';
import '../error/failures.dart';

class DioFailureMapper {
  static Failure map(Object error) {
    if (error is DioException) {
      final code = error.response?.statusCode;
      if (code == 401) {
        return const ServerFailure('Session expired. Try again.');
      }

      if (code == 403) return const ServerFailure('Access denied.');
      if (code == 404) return const ServerFailure('Not found.');
      if (code != null && code >= 500) {
        return const ServerFailure('Server error. Try again.');
      }
      if (error.type == DioExceptionType.connectionTimeout ||
          error.type == DioExceptionType.sendTimeout ||
          error.type == DioExceptionType.receiveTimeout) {
        return const ConnectionFailure('Connection timeout. Try again.');
      }

      if (error.type == DioExceptionType.connectionError) {
        return const ConnectionFailure('No internet connection. Try again.');
      }

      final data = error.response?.data;
      if (data is Map && data['message'] != null) {
        return ServerFailure(data['message'].toString());
      }

      return const ServerFailure('Request failed. Try again.');
    }

    return const ServerFailure('Something went wrong. Try again.');
  }
}
