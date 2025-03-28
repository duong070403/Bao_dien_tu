﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTL_BaoDienTu.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    category_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    category_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Categori__D54EE9B464DC374C", x => x.category_id);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    subscription_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    subscribed_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Subscrip__863A7EC1CA7BC579", x => x.subscription_id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    full_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__B9BE370FCC5C26E1", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "News",
                columns: table => new
                {
                    news_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    author_id = table.Column<int>(type: "int", nullable: false),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    is_approved = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__News__4C27CCD8946A0AC3", x => x.news_id);
                    table.ForeignKey(
                        name: "FK__News__author_id__412EB0B6",
                        column: x => x.author_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK__News__category_i__4222D4EF",
                        column: x => x.category_id,
                        principalTable: "Categories",
                        principalColumn: "category_id");
                });

            migrationBuilder.CreateTable(
                name: "NewsSharing",
                columns: table => new
                {
                    share_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    news_id = table.Column<int>(type: "int", nullable: false),
                    sender_id = table.Column<int>(type: "int", nullable: false),
                    receiver_email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    shared_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__NewsShar__C36E8AE5286BFCE6", x => x.share_id);
                    table.ForeignKey(
                        name: "FK__NewsShari__news___48CFD27E",
                        column: x => x.news_id,
                        principalTable: "News",
                        principalColumn: "news_id");
                    table.ForeignKey(
                        name: "FK__NewsShari__sende__49C3F6B7",
                        column: x => x.sender_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "UQ__Categori__5189E2553B2E3EC2",
                table: "Categories",
                column: "category_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_News_author_id",
                table: "News",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_News_category_id",
                table: "News",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_NewsSharing_news_id",
                table: "NewsSharing",
                column: "news_id");

            migrationBuilder.CreateIndex(
                name: "IX_NewsSharing_sender_id",
                table: "NewsSharing",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__AB6E616488E3A22A",
                table: "Users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsSharing");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "News");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
