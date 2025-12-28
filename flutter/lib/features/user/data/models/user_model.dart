class UserModel {
  final String firstName;
  final String lastName;
  final String email;
  final RoleModel role;

  UserModel({
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.role,
  });

  String get fullName => '$firstName $lastName';

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      firstName: json['firstName'] ?? '',
      lastName: json['lastName'] ?? '',
      email: json['email'] ?? '',
      role: RoleModel.fromJson(json['role'] ?? {}),
    );
  }
}

class RoleModel {
  final int id;
  final String name;
  final String description;

  RoleModel({
    required this.id,
    required this.name,
    required this.description,
  });

  factory RoleModel.fromJson(Map<String, dynamic> json) {
    return RoleModel(
      id: json['id'] ?? 0,
      name: json['name'] ?? '',
      description: json['description'] ?? '',
    );
  }
}
