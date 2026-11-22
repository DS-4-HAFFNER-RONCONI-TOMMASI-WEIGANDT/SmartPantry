namespace TP04.Permissions;

public static class TP04Permissions
{
    public const string GroupName = "TP04";


    public static class Books
    {
        public const string Default = GroupName + ".Books";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class Authors
    {
        public const string Default = GroupName + ".Authors";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Catálogo de productos compartido: cualquier usuario autenticado consulta y guarda,
    // pero editar y borrar productos internos (que usan otras despensas) es de admin.
    public static class Products
    {
        public const string Manage = GroupName + ".Products.Manage";
    }

    // HU-05: métricas de la integración externa (Open Food Facts) para el administrador.
    public static class IntegrationMetrics
    {
        public const string Default = GroupName + ".IntegrationMetrics";
    }

    // Consulta de la auditoría por parte del administrador.
    public static class AuditLogs
    {
        public const string Default = GroupName + ".AuditLogs";
    }

    //Add your own permission names. Example:
    //public const string MyPermission1 = GroupName + ".MyPermission1";
}
