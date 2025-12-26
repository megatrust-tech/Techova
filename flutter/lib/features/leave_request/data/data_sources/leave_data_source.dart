import 'package:dio/dio.dart';
import 'package:file_picker/file_picker.dart';

import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/dio_client.dart';
import '../models/conflict_check_model.dart';
import '../models/leave_request_model.dart';
import '../models/leave_type_model.dart';

/// Data source for leave-related API calls
class LeaveDataSource {
  final DioClient _dioClient;

  LeaveDataSource(this._dioClient);

  /// Fetches available leave types from API
  Future<List<LeaveTypeModel>> getLeaveTypes() async {
    final response = await _dioClient.get(ApiEndpoints.leaveTypes);
    final data = response.data as List<dynamic>;
    return data
        .map((json) => LeaveTypeModel.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  /// Checks for conflicts with the given date range
  Future<ConflictCheckResponse> checkConflict({
    required DateTime startDate,
    required DateTime endDate,
  }) async {
    final response = await _dioClient.get(
      ApiEndpoints.checkConflict,
      queryParameters: {
        'startDate': startDate.toIso8601String(),
        'endDate': endDate.toIso8601String(),
      },
    );
    return ConflictCheckResponse.fromJson(response.data as Map<String, dynamic>);
  }

  /// Submits a leave request with optional file attachment
  Future<LeaveRequestResponse> submitLeaveRequest({
    required LeaveRequestModel request,
    PlatformFile? attachment,
  }) async {
    // Always use form-data format as expected by the API
    final formDataMap = <String, dynamic>{
      'Type': request.leaveType, // Integer value (0, 1, 2...)
      'StartDate': request.startDate.toIso8601String(),
      'EndDate': request.endDate.toIso8601String(),
    };

    if (request.notes != null && request.notes!.isNotEmpty) {
      formDataMap['Notes'] = request.notes;
    }

    if (attachment != null) {
      formDataMap['Attachment'] = await MultipartFile.fromFile(
        attachment.path!,
        filename: attachment.name,
      );
    }

    final formData = FormData.fromMap(formDataMap);

    final response = await _dioClient.post(
      ApiEndpoints.leaves,
      data: formData,
      options: Options(
        contentType: 'multipart/form-data',
      ),
    );
    return LeaveRequestResponse.fromJson(response.data as Map<String, dynamic>);
  }
}
