import 'package:equatable/equatable.dart';

/// Model representing a leave status option from the backend
class LeaveStatusModel extends Equatable {
  final String name;
  final int value;

  const LeaveStatusModel({
    required this.name,
    required this.value,
  });

  factory LeaveStatusModel.fromJson(Map<String, dynamic> json) {
    return LeaveStatusModel(
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
