import 'package:equatable/equatable.dart';

/// Individual leave item for calendar view
class CalendarLeaveDto extends Equatable {
  final int id;
  final int employeeId;
  final String employeeName;
  final String leaveType;
  final DateTime startDate;
  final DateTime endDate;
  final int numberOfDays;

  const CalendarLeaveDto({
    required this.id,
    required this.employeeId,
    required this.employeeName,
    required this.leaveType,
    required this.startDate,
    required this.endDate,
    required this.numberOfDays,
  });

  factory CalendarLeaveDto.fromJson(Map<String, dynamic> json) {
    return CalendarLeaveDto(
      id: json['id'] as int? ?? 0,
      employeeId: json['employeeId'] as int? ?? 0,
      employeeName: json['employeeName'] as String? ?? '',
      leaveType: json['leaveType'] as String? ?? '',
      startDate: DateTime.tryParse(json['startDate']?.toString() ?? '') ?? DateTime.now(),
      endDate: DateTime.tryParse(json['endDate']?.toString() ?? '') ?? DateTime.now(),
      numberOfDays: json['numberOfDays'] as int? ?? 0,
    );
  }

  @override
  List<Object?> get props => [id, employeeId, employeeName, leaveType, startDate, endDate, numberOfDays];
}

/// Manager group with leaves (for HR/Admin view)
class CalendarGroupedByManagerDto extends Equatable {
  final int managerId;
  final String managerName;
  final int? departmentId;
  final List<CalendarLeaveDto> leaves;

  const CalendarGroupedByManagerDto({
    required this.managerId,
    required this.managerName,
    this.departmentId,
    required this.leaves,
  });

  factory CalendarGroupedByManagerDto.fromJson(Map<String, dynamic> json) {
    final leavesJson = json['leaves'] as List<dynamic>? ?? [];
    return CalendarGroupedByManagerDto(
      managerId: json['managerId'] as int? ?? 0,
      managerName: json['managerName'] as String? ?? '',
      departmentId: json['departmentId'] as int?,
      leaves: leavesJson
          .map((e) => CalendarLeaveDto.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  @override
  List<Object?> get props => [managerId, managerName, departmentId, leaves];
}

/// Wrapper response that handles both flat and grouped responses
class CalendarDataResponse extends Equatable {
  final List<CalendarLeaveDto>? leaves;
  final List<CalendarGroupedByManagerDto>? groupedByManager;

  const CalendarDataResponse({
    this.leaves,
    this.groupedByManager,
  });

  /// Returns true if this is a grouped response (HR/Admin view)
  bool get isGrouped => groupedByManager != null && groupedByManager!.isNotEmpty;

  /// Returns all leaves flattened (for calendar markers)
  List<CalendarLeaveDto> get allLeaves {
    if (leaves != null) {
      return leaves!;
    }
    if (groupedByManager != null) {
      return groupedByManager!.expand((g) => g.leaves).toList();
    }
    return [];
  }

  /// Returns leaves for a specific date
  List<CalendarLeaveDto> getLeavesForDate(DateTime date) {
    final dateOnly = DateTime(date.year, date.month, date.day);
    return allLeaves.where((leave) {
      final start = DateTime(leave.startDate.year, leave.startDate.month, leave.startDate.day);
      final end = DateTime(leave.endDate.year, leave.endDate.month, leave.endDate.day);
      return !dateOnly.isBefore(start) && !dateOnly.isAfter(end);
    }).toList();
  }

  /// Returns list of dates that have leaves
  Set<DateTime> get datesWithLeaves {
    final dates = <DateTime>{};
    for (final leave in allLeaves) {
      var current = DateTime(leave.startDate.year, leave.startDate.month, leave.startDate.day);
      final end = DateTime(leave.endDate.year, leave.endDate.month, leave.endDate.day);
      while (!current.isAfter(end)) {
        dates.add(current);
        current = current.add(const Duration(days: 1));
      }
    }
    return dates;
  }

  factory CalendarDataResponse.fromJson(Map<String, dynamic> json) {
    List<CalendarLeaveDto>? leaves;
    List<CalendarGroupedByManagerDto>? grouped;

    if (json['leaves'] != null) {
      final leavesJson = json['leaves'] as List<dynamic>;
      leaves = leavesJson
          .map((e) => CalendarLeaveDto.fromJson(e as Map<String, dynamic>))
          .toList();
    }

    if (json['groupedByManager'] != null) {
      final groupedJson = json['groupedByManager'] as List<dynamic>;
      grouped = groupedJson
          .map((e) => CalendarGroupedByManagerDto.fromJson(e as Map<String, dynamic>))
          .toList();
    }

    return CalendarDataResponse(
      leaves: leaves,
      groupedByManager: grouped,
    );
  }

  @override
  List<Object?> get props => [leaves, groupedByManager];
}
