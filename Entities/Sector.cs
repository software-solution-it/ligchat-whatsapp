using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WhatsAppProject.Entities
{
    [Table("setores")] // Define o nome da tabela em snake_case
    public class Sector
    {
        [Key]
        [Column("id")] // Define o nome da coluna em snake_case
        public int Id { get; set; }

        [Column("nome_setor")] // Define o nome da coluna em snake_case
        public string? Nome { get; set; }

        [Column("descricao_setor")] // Define o nome da coluna em snake_case
        public string? Descricao { get; set; }

        [Column("id_negocio_usuario")] // Define o nome da coluna em snake_case
        public int? IdNegocioUsuario { get; set; }

        [Column("numero")] // Define o nome da coluna em snake_case
        public string? PhoneNumberId { get; set; }

        [Column("token_acesso")] // Define o nome da coluna em snake_case
        public string? AccessToken { get; set; }

        [Column("data_criacao")] // Define o nome da coluna em snake_case
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("data_atualizacao")] // Define o nome da coluna em snake_case
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("status")] // Define o nome da coluna em snake_case
        public int? Status { get; set; }

    }
}
