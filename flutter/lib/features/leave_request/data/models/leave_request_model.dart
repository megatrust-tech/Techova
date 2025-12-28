import 'package:equatable/equatable.dart';

/// Model for submitting a leave request
class LeaveRequestModel extends Equatable {
  final int leaveType;
  final DateTime startDate;
  final DateTime endDate;
  final String? notes;
  final String? attachmentUrl;

  const LeaveRequestModel({
    required this.leaveType,
    required this.startDate,
    required this.endDate,
    this.notes,
    this.attachmentUrl,
  });

  /// Calculate number of days between start and end date
  int get numberOfDays {
    return endDate.difference(startDate).inDays;
  }

  Map<String, dynamic> toJson() {
    return {
      'leaveType': leaveType,
      'startDate': startDate.toIso8601String(),
      'endDate': endDate.toIso8601String(),
      'notes': notes,
      'attachmentUrl': attachmentUrl,
    };
  }

  @override
  List<Object?> get props => [leaveType, startDate, endDate, notes, attachmentUrl];
}

/// Model for leave request response from API
class LeaveRequestResponse extends Equatable {
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
  final String employeeEmail;

  const LeaveRequestResponse({
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
    required this.employeeEmail,
  });

  factory LeaveRequestResponse.fromJson(Map<String, dynamic> json) {
    return LeaveRequestResponse(
      id: json['id'] as int,
      leaveType: json['leaveType'] as String,
      startDate: DateTime.parse(json['startDate'] as String),
      endDate: DateTime.parse(json['endDate'] as String),
      numberOfDays: json['numberOfDays'] as int,
      status: json['status'] as String,
      notes: json['notes'] as String?,
      attachmentUrl: json['attachmentUrl'] as String?,
      managerId: json['managerId'] as int,
      createdAt: DateTime.parse(json['createdAt'] as String),
      employeeEmail: json['employeeEmail'] as String,
    );
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
