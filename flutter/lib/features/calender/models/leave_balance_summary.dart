class LeaveBalanceSummary {
  final String? leaveType;
  final int? remaining;

  LeaveBalanceSummary({this.leaveType, this.remaining});

  factory LeaveBalanceSummary.fromJson(Map<String, dynamic> json) {
    return LeaveBalanceSummary(
      leaveType: json['leaveType']?.toString(),
      remaining: json['remaining'] is int ? json['remaining'] as int : int.tryParse('${json['remaining']}'),
    );
  }
}
