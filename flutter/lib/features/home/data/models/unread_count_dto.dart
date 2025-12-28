class UnreadCountDto {
  final int count;

  const UnreadCountDto({required this.count});

  factory UnreadCountDto.fromJson(Map<String, dynamic> json) {
    final v = json['count'] ?? json['unreadCount'] ?? json['unread'] ?? 0;
    return UnreadCountDto(count: (v as num).toInt());
  }
}
