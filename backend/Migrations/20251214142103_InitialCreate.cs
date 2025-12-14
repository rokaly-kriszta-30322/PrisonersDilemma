using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_data",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    nr_turns = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    game_nr = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_data", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "bot_strat",
                columns: table => new
                {
                    bot_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    start = table.Column<bool>(type: "bit", nullable: false),
                    strategy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    money = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bot_strat", x => x.bot_id);
                    table.ForeignKey(
                        name: "FK_bot_strat_user_data_user_id",
                        column: x => x.user_id,
                        principalTable: "user_data",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "game_data",
                columns: table => new
                {
                    gm_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    m_points = table.Column<int>(type: "int", nullable: false),
                    coopcoop = table.Column<int>(type: "int", nullable: false),
                    coopdeflect = table.Column<int>(type: "int", nullable: false),
                    deflectcoop = table.Column<int>(type: "int", nullable: false),
                    deflectdeflect = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_data", x => x.gm_id);
                    table.ForeignKey(
                        name: "FK_game_data_user_data_user_id",
                        column: x => x.user_id,
                        principalTable: "user_data",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "game_session",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user1_id = table.Column<int>(type: "int", nullable: false),
                    choice_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    game_nr = table.Column<int>(type: "int", nullable: false),
                    m_points = table.Column<int>(type: "int", nullable: false),
                    coopcoop = table.Column<int>(type: "int", nullable: false),
                    coopdeflect = table.Column<int>(type: "int", nullable: false),
                    deflectcoop = table.Column<int>(type: "int", nullable: false),
                    deflectdeflect = table.Column<int>(type: "int", nullable: false),
                    user2_id = table.Column<int>(type: "int", nullable: false),
                    choice_type2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    game2_nr = table.Column<int>(type: "int", nullable: false),
                    m_points2 = table.Column<int>(type: "int", nullable: false),
                    coopcoop2 = table.Column<int>(type: "int", nullable: false),
                    coopdeflect2 = table.Column<int>(type: "int", nullable: false),
                    deflectcoop2 = table.Column<int>(type: "int", nullable: false),
                    deflectdeflect2 = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_session", x => x.ID);
                    table.ForeignKey(
                        name: "FK_game_session_user_data_user1_id",
                        column: x => x.user1_id,
                        principalTable: "user_data",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_game_session_user_data_user2_id",
                        column: x => x.user2_id,
                        principalTable: "user_data",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pending_interactions",
                columns: table => new
                {
                    pending_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    target_id = table.Column<int>(type: "int", nullable: false),
                    user_choice = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    target_choice = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pending_interactions", x => x.pending_id);
                    table.ForeignKey(
                        name: "FK_pending_interactions_user_data_target_id",
                        column: x => x.target_id,
                        principalTable: "user_data",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pending_interactions_user_data_user_id",
                        column: x => x.user_id,
                        principalTable: "user_data",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bot_strat_user_id",
                table: "bot_strat",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_data_user_id",
                table: "game_data",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_session_user1_id",
                table: "game_session",
                column: "user1_id");

            migrationBuilder.CreateIndex(
                name: "IX_game_session_user2_id",
                table: "game_session",
                column: "user2_id");

            migrationBuilder.CreateIndex(
                name: "IX_pending_interactions_target_id",
                table: "pending_interactions",
                column: "target_id");

            migrationBuilder.CreateIndex(
                name: "IX_pending_interactions_user_id",
                table: "pending_interactions",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bot_strat");

            migrationBuilder.DropTable(
                name: "game_data");

            migrationBuilder.DropTable(
                name: "game_session");

            migrationBuilder.DropTable(
                name: "pending_interactions");

            migrationBuilder.DropTable(
                name: "user_data");
        }
    }
}
