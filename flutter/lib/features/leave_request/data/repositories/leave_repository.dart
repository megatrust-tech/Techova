import 'package:dio/dio.dart';
import 'package:file_picker/file_picker.dart';

import '../data_sources/leave_data_source.dart';
import '../models/conflict_check_model.dart';
import '../models/leave_request_model.dart';
import '../models/leave_type_model.dart';

/// Repository for leave operations with caching support
class LeaveRepository {
  final LeaveDataSource _dataSource;
  
  // In-memory cache for leave types (rarely change)
  List<LeaveTypeModel>? _cachedLeaveTypes;

  LeaveRepository(this._dataSource);

  /// Gets leave types, using cache if available
  Future<List<LeaveTypeModel>> getLeaveTypes({bool forceRefresh = false}) async {
    if (!forceRefresh && _cachedLeaveTypes != null) {
      return _cachedLeaveTypes!;
    }

    try {
      _cachedLeaveTypes = await _dataSource.getLeaveTypes();
      return _cachedLeaveTypes!;
    } on DioException {
      rethrow;
    }
  }

  /// Checks for conflicts with the given date range
  Future<ConflictCheckResponse> checkConflict({
    required DateTime startDate,
    required DateTime endDate,
  }) async {
    try {
      return await _dataSource.checkConflict(
        startDate: startDate,
        endDate: endDate,
      );
    } on DioException {
      rethrow;
    }
  }

  /// Submits a leave request
  Future<LeaveRequestResponse> submitLeaveRequest({
    required LeaveRequestModel request,
    PlatformFile? attachment,
  }) async {
    try {
      return await _dataSource.submitLeaveRequest(
        request: request,
        attachment: attachment,
      );
    } on DioException {
      rethrow;
    }
  }

  /// Clears the leave types cache
  void clearCache() {
    _cachedLeaveTypes = null;
  }
}
