﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TripAppServer
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class smart_trip_dbEntities : DbContext
    {
        public smart_trip_dbEntities()
            : base("name=smart_trip_dbEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<sites> sites { get; set; }
        public virtual DbSet<users> users { get; set; }
        public virtual DbSet<user_paths> user_paths { get; set; }
    }
}