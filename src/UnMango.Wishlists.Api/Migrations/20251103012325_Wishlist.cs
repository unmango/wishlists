using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnMango.Wishlists.Api.Migrations
{
	/// <inheritdoc />
	public partial class Wishlist : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder) {
			migrationBuilder.CreateTable(
				name: "Wishlists",
				columns: table => new {
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					Name = table.Column<string>(type: "text", nullable: false),
					OwnerId = table.Column<Guid>(type: "uuid", nullable: false)
				},
				constraints: table => {
					table.PrimaryKey("PK_Wishlists", x => x.Id);
					table.ForeignKey(
						name: "FK_Wishlists_AspNetUsers_OwnerId",
						column: x => x.OwnerId,
						principalTable: "AspNetUsers",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_Wishlists_OwnerId",
				table: "Wishlists",
				column: "OwnerId");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder) {
			migrationBuilder.DropTable(
				name: "Wishlists");
		}
	}
}
