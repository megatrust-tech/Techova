import 'package:equatable/equatable.dart';

/// Model for leave request audit/history log entries
class LeaveAuditLogDto extends Equatable {
  final int id;
  final String action;
  final String actionBy;
  final String newStatus;
  final String? comment;
  final DateTime actionDate;

  const LeaveAuditLogDto({
    required this.id,
    required this.action,
    required this.actionBy,
    required this.newStatus,
    this.comment,
    required this.actionDate,
  });

  factory LeaveAuditLogDto.fromJson(Map<String, dynamic> json) {
    return LeaveAuditLogDto(
      id: json['id'] as int? ?? 0,
      action: json['action'] as String? ?? '',
      actionBy: json['actionBy'] as String? ?? '',
      newStatus: json['newStatus'] as String? ?? '',
      comment: json['comment'] as String?,
      actionDate: DateTime.tryParse(json['actionDate']?.toString() ?? '') ?? DateTime.now(),
    );
  }

  @override
  List<Object?> get props => [id, action, actionBy, newStatus, comment, actionDate];
}
