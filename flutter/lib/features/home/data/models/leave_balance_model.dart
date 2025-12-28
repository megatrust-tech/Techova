import 'package:equatable/equatable.dart';

/// Model for leave balance item
class LeaveBalanceModel extends Equatable {
  final String type;
  final int totalDays;
  final int usedDays;
  final int remainingDays;

  const LeaveBalanceModel({
    required this.type,
    required this.totalDays,
    required this.usedDays,
    required this.remainingDays,
  });

  factory LeaveBalanceModel.fromJson(Map<String, dynamic> json) {
    return LeaveBalanceModel(
      type: json['type'] as String,
      totalDays: json['totalDays'] as int,
      usedDays: json['usedDays'] as int,
      remainingDays: json['remainingDays'] as int,
    );
  }

  /// Get usage percentage (0.0 to 1.0)
  double get usagePercent {
    if (totalDays == 0) return 0.0;
    return usedDays / totalDays;
  }

  @override
  List<Object?> get props => [type, totalDays, usedDays, remainingDays];
}
