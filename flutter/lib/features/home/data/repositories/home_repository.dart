import 'package:dio/dio.dart';

import '../data_sources/home_data_source.dart';
import '../models/leave_balance_model.dart';
import '../../../leaves/data/models/leave_item_model.dart';

/// Repository for home screen data
class HomeRepository {
  final HomeDataSource _dataSource;
  
  // Cache for leave balance
  List<LeaveBalanceModel>? _cachedBalance;

  HomeRepository(this._dataSource);

  /// Gets remaining leave balance
  Future<List<LeaveBalanceModel>> getRemainingLeaves({bool forceRefresh = false}) async {
    if (!forceRefresh && _cachedBalance != null) {
      return _cachedBalance!;
    }

    try {
      _cachedBalance = await _dataSource.getRemainingLeaves();
      return _cachedBalance!;
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

  /// Clear cache
  void clearCache() {
    _cachedBalance = null;
  }
}
