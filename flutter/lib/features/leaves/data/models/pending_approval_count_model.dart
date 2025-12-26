import 'package:equatable/equatable.dart';

/// Model for pending approval count response
class PendingApprovalCount extends Equatable {
  final int pendingManagerApproval;
  final int pendingHRApproval;
  final int totalPending;

  const PendingApprovalCount({
    required this.pendingManagerApproval,
    required this.pendingHRApproval,
    required this.totalPending,
  });

  factory PendingApprovalCount.fromJson(Map<String, dynamic> json) {
    return PendingApprovalCount(
      pendingManagerApproval: json['pendingManagerApproval'] as int? ?? 0,
      pendingHRApproval: json['pendingHRApproval'] as int? ?? 0,
      totalPending: json['totalPending'] as int? ?? 0,
    );
  }

  @override
  List<Object?> get props => [pendingManagerApproval, pendingHRApproval, totalPending];
}
