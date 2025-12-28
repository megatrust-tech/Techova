import '../error/failures.dart';

// A generic sealed class to handle Success and Failure states
sealed class ApiResult<T> {
  const ApiResult();
}

class Success<T> extends ApiResult<T> {
  final T data;
  const Success(this.data);
}

class Error<T> extends ApiResult<T> {
  final Failure failure;
  const Error(this.failure);
}