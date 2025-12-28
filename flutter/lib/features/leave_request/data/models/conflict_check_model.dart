import 'package:equatable/equatable.dart';

/// Model for conflict check API response
class ConflictCheckResponse extends Equatable {
  final bool hasConflict;
  final String? conflictingEmployeeName;
  final String message;

  const ConflictCheckResponse({
    required this.hasConflict,
    this.conflictingEmployeeName,
    required this.message,
  });

  factory ConflictCheckResponse.fromJson(Map<String, dynamic> json) {
    return ConflictCheckResponse(
      hasConflict: json['hasConflict'] as bool? ?? false,
      conflictingEmployeeName: json['conflictingEmployeeName'] as String?,
      message: json['message'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'hasConflict': hasConflict,
      'conflictingEmployeeName': conflictingEmployeeName,
      'message': message,
    };
  }

  @override
  List<Object?> get props => [hasConflict, conflictingEmployeeName, message];
}
