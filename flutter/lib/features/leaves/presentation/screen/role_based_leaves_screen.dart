import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../user/presentation/cubit/user_cubit.dart';
import 'employee_leaves_screen.dart';
import 'pending_approvals_screen.dart';

/// Role-based leaves screen that shows different views based on user role
/// - Employee (roleId: 1): Shows their own leaves with status filter
/// - HR (roleId: 2): Shows pending HR approvals
/// - Manager (roleId: 3): Shows pending manager approvals
class RoleBasedLeavesScreen extends StatelessWidget {
  const RoleBasedLeavesScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<UserCubit, UserState>(
      builder: (context, state) {
        if (state is UserLoaded) {
          final roleId = state.user.role.id;
          
          switch (roleId) {
            case 2: // HR
              return const PendingApprovalsScreen(
                statusFilter: 'PendingHR',
                title: 'Pending HR Approvals',
              );
            case 3: // Manager
              return const PendingApprovalsScreen(
                statusFilter: 'PendingManager',
                title: 'Pending Approvals',
              );
            case 1: // Employee
            default:
              return const EmployeeLeavesScreen();
          }
        }
        
        // If user not loaded, show employee view as default
        return const EmployeeLeavesScreen();
      },
    );
  }
}
