<?php

declare(strict_types=1);

use App\Enums\TaskStatusEnum;
use App\Enums\TaskTypeEnum;
use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('tasks', function (Blueprint $table): void {
            $table->uuid('id')->primary();
            $table->string('name', 150);
            $table->enum('type', TaskTypeEnum::values());
            $table->text('description')->nullable();
            $table->enum('status', TaskStatusEnum::values())->default(TaskStatusEnum::Pending->value);
            // Bumped on every change (admin edit or accepted device update); devices send it back as base_version.
            $table->unsignedInteger('version')->default(1);
            $table->timestamps();
            $table->softDeletes();

            $table->index(['type', 'status']);
            $table->index('updated_at');
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('tasks');
    }
};
