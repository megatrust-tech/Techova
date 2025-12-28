import 'package:dio/dio.dart';

import '../data_sources/home_data_source.dart';
import '../models/leave_balance_model.dart';
import '../../../leaves/data/models/leave_item_model.dart';
import '../models/unread_count_dto.dart';

/// Repository for home screen data
class HomeRepository {
  final HomeDataSource _dataSource;

  HomeRepository(this._dataSource);

  /// Gets remaining leave balance (always fresh - no caching for accuracy)
  Future<List<LeaveBalanceModel>> getRemainingLeaves() async {
    try {
      return await _dataSource.getRemainingLeaves();
    } on DioException {
      rethrow;
    }
  }

  /// Gets recent leaves
  Future<List<LeaveItem>> getRecentLeaves({int limit = 5}) async {
    try {
      return await _dataSource.getRecentLeaves(limit: limit);
    } on DioException {
      rethrow;
    }
  }

  Future<int> getUnreadCount() async {
    final dto = await _dataSource.getUnreadCount();
    return dto.count;
  }


}

