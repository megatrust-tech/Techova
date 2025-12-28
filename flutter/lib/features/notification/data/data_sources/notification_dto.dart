import 'package:json_annotation/json_annotation.dart';

part 'notification_dto.g.dart';

@JsonSerializable()
class NotificationDto {
  final int id;
  final String title;
  final String message;
  final bool isRead;
  final DateTime createdAt;
  final String timeAgo;

  const NotificationDto({
    required this.id,
    required this.title,
    required this.message,
    required this.isRead,
    required this.createdAt,
    required this.timeAgo,
  });

  factory NotificationDto.fromJson(Map<String, dynamic> json) =>
      _$NotificationDtoFromJson(json);

  Map<String, dynamic> toJson() => _$NotificationDtoToJson(this);
}
