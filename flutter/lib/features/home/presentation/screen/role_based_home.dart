import 'package:flutter/material.dart';

import '../../../../core/servicelocator/servicelocator.dart';
import '../../../auth/services/token_storage_service.dart';
import 'employee_home_screen.dart';
import 'manager_home_screen.dart';

/// Widget that displays the appropriate home screen based on user role
/// 
/// Role IDs:
/// - 4: Employee -> EmployeeHomeScreen
/// - Other (HR/Manager): ManagerHomeScreen
class RoleBasedHome extends StatefulWidget {
  const RoleBasedHome({super.key});

  @override
  State<RoleBasedHome> createState() => _RoleBasedHomeState();
}

class _RoleBasedHomeState extends State<RoleBasedHome> {
  int? _roleId;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadRole();
  }

  Future<void> _loadRole() async {
    final tokenStorage = sl<TokenStorageService>();
    final roleId = await tokenStorage.getRoleId();
    
    if (mounted) {
      setState(() {
        _roleId = roleId;
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Scaffold(
        body: Center(
          child: CircularProgressIndicator(),
        ),
      );
    }

    // Employee role ID is 4
    // HR/Manager have other role IDs (e.g., 1, 2, 3)
    if (_roleId == 4) {
      return const EmployeeHomeScreen();
    } else {
      return const ManagerHomeScreen();
    }
  }
}
