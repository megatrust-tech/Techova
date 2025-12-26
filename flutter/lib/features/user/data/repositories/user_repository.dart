import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/dio_client.dart';
import '../models/user_model.dart';

class UserRepository {
  final DioClient _dioClient;

  UserRepository(this._dioClient);

  /// Fetch user details by ID
  Future<UserModel> getUserById(int userId) async {
    try {
      final response = await _dioClient.get(ApiEndpoints.userById(userId));
      return UserModel.fromJson(response.data);
    } catch (e) {
      rethrow;
    }
  }
}
