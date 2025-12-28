import 'package:equatable/equatable.dart';

/// Model representing a leave type option from the backend
class LeaveTypeModel extends Equatable {
  final String name;
  final int value;

  const LeaveTypeModel({
    required this.name,
    required this.value,
  });

  factory LeaveTypeModel.fromJson(Map<String, dynamic> json) {
    return LeaveTypeModel(
      name: json['name'] as String,
      value: json['value'] as int,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'name': name,
      'value': value,
    };
  }

  @override
  List<Object?> get props => [name, value];
}
