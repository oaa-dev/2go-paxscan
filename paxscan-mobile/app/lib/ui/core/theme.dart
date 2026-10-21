import 'package:flutter/material.dart';
import 'package:paxscan_core/paxscan_core.dart';

abstract final class AppTheme {
  static const _seed = Color(0xFF1F6FEB);

  static ThemeData light() => ThemeData(colorSchemeSeed: _seed, useMaterial3: true);

  static ThemeData dark() => ThemeData(colorSchemeSeed: _seed, brightness: Brightness.dark, useMaterial3: true);

  static Color statusColor(TaskStatus status, ColorScheme scheme) => switch (status) {
    TaskStatus.pending => scheme.outline,
    TaskStatus.inProgress => Colors.orange.shade700,
    TaskStatus.done => Colors.green.shade700,
  };

  static IconData typeIcon(TaskType type) => switch (type) {
    TaskType.chef => Icons.restaurant,
    TaskType.guard => Icons.shield_outlined,
  };
}
