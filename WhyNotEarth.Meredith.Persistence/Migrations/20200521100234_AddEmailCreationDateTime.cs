﻿using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace WhyNotEarth.Meredith.Persistence.Migrations
{
    public partial class AddEmailCreationDateTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreationDateTime",
                schema: "public",
                table: "EmailRecipients",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreationDateTime",
                schema: "public",
                table: "EmailRecipients");
        }
    }
}
