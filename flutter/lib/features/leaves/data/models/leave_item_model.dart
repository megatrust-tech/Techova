import 'package:equatable/equatable.dart';

/// Model for a single leave request item
class LeaveItem extends Equatable {
  final int id;
  final String leaveType;
  final DateTime startDate;
  final DateTime endDate;
  final int numberOfDays;
  final String status;
  final String? notes;
  final String? attachmentUrl;
  final int managerId;
  final DateTime createdAt;
  final String? employeeEmail;

  const LeaveItem({
    required this.id,
    required this.leaveType,
    required this.startDate,
    required this.endDate,
    required this.numberOfDays,
    required this.status,
    this.notes,
    this.attachmentUrl,
    required this.managerId,
    required this.createdAt,
    this.employeeEmail,
  });

  factory LeaveItem.fromJson(Map<String, dynamic> json) {
    return LeaveItem(
      id: json['id'] as int,
      leaveType: json['leaveType'] as String? ?? 'Unknown',
      startDate: DateTime.parse(json['startDate'] as String),
      endDate: DateTime.parse(json['endDate'] as String),
      numberOfDays: json['numberOfDays'] as int? ?? 0,
      status: json['status'] as String? ?? 'Unknown',
      notes: json['notes'] as String?,
      attachmentUrl: json['attachmentUrl'] as String?,
      managerId: json['managerId'] as int? ?? 0,
      createdAt: DateTime.parse(json['createdAt'] as String),
      employeeEmail: json['employeeEmail'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'leaveType': leaveType,
      'startDate': startDate.toIso8601String(),
      'endDate': endDate.toIso8601String(),
      'numberOfDays': numberOfDays,
      'status': status,
      'notes': notes,
      'attachmentUrl': attachmentUrl,
      'managerId': managerId,
      'createdAt': createdAt.toIso8601String(),
      'employeeEmail': employeeEmail,
    };
  }

  @override
  List<Object?> get props => [
        id,
        leaveType,
        startDate,
        endDate,
        numberOfDays,
        status,
        notes,
        attachmentUrl,
        managerId,
        createdAt,
        employeeEmail,
      ];
}

/// Paginated response model for leave requests
class PaginatedLeavesResponse extends Equatable {
  final List<LeaveItem> items;
  final int pageNumber;
  final int pageSize;
  final int totalCount;
  final int totalPages;

  const PaginatedLeavesResponse({
    required this.items,
    this.pageNumber = 1,
    this.pageSize = 10,
    this.totalCount = 0,
    this.totalPages = 0,
  });

  factory PaginatedLeavesResponse.fromJson(Map<String, dynamic> json) {
    return PaginatedLeavesResponse(
      items: (json['items'] as List<dynamic>?)
              ?.map((e) => LeaveItem.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [],
      pageNumber: json['pageNumber'] as int? ?? 1,
      pageSize: json['pageSize'] as int? ?? 10,
      totalCount: json['totalCount'] as int? ?? 0,
      totalPages: json['totalPages'] as int? ?? 0,
    );
  }

  bool get hasMore => pageNumber < totalPages;

  @override
  List<Object?> get props => [items, pageNumber, pageSize, totalCount, totalPages];
}
