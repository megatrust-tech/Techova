class PaginatedLeaveResponse {
  final List<LeaveRequestModels> items;
  final int? totalCount;
  final int pageNumber;
  final int pageSize;
  final int totalPages;

  PaginatedLeaveResponse({
    required this.items,
    required this.totalCount,
    required this.pageNumber,
    required this.pageSize,
    required this.totalPages,
  });

  factory PaginatedLeaveResponse.fromJson(Map<String, dynamic> json) {
    final rawItems = (json['items'] as List?) ?? [];
    return PaginatedLeaveResponse(
      items: rawItems.map((e) => LeaveRequestModels.fromJson((e as Map).cast<String, dynamic>())).toList(),
      totalCount: json['totalCount'] as int?,
      pageNumber: (json['pageNumber'] as int?) ?? 1,
      pageSize: (json['pageSize'] as int?) ?? 10,
      totalPages: (json['totalPages'] as int?) ?? 1,
    );
  }
}

class LeaveRequestModels {
  final int? id;
  final String? leaveType;
  final DateTime? startDate;
  final DateTime? endDate;
  final int? numberOfDays;
  final String? status;
  final String? notes;
  final String? attachmentUrl;
  final int? managerId;
  final DateTime? createdAt;
  final String? employeeEmail;

  LeaveRequestModels({
    required this.id,
    required this.leaveType,
    required this.startDate,
    required this.endDate,
    required this.numberOfDays,
    required this.status,
    required this.notes,
    required this.attachmentUrl,
    required this.managerId,
    required this.createdAt,
    required this.employeeEmail,
  });

  factory LeaveRequestModels.fromJson(Map<String, dynamic> json) {
    DateTime? parseDate(dynamic v) {
      if (v == null) return null;
      return DateTime.tryParse(v.toString());
    }

    int? parseInt(dynamic v) {
      if (v == null) return null;
      if (v is int) return v;
      return int.tryParse(v.toString());
    }

    return LeaveRequestModels(
      id: parseInt(json['id']),
      leaveType: json['leaveType']?.toString(),
      startDate: parseDate(json['startDate']),
      endDate: parseDate(json['endDate']),
      numberOfDays: parseInt(json['numberOfDays']),
      status: json['status']?.toString(),
      notes: json['notes']?.toString(),
      attachmentUrl: json['attachmentUrl']?.toString(),
      managerId: parseInt(json['managerId']),
      createdAt: parseDate(json['createdAt']),
      employeeEmail: json['employeeEmail']?.toString(),
    );
  }
}
