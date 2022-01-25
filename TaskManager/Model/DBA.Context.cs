﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TaskManager.Model
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    
    public partial class Entities : DbContext
    {
        public Entities()
            : base("name=Entities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<ExecutionTime> ExecutionTimes { get; set; }
        public virtual DbSet<Status> Status { get; set; }
        public virtual DbSet<Task> Tasks { get; set; }
    
        [DbFunction("Entities", "fnGetFlatList")]
        public virtual IQueryable<fnGetFlatList_Result> fnGetFlatList(Nullable<int> in_IDTask, Nullable<int> in_Type)
        {
            var in_IDTaskParameter = in_IDTask.HasValue ?
                new ObjectParameter("in_IDTask", in_IDTask) :
                new ObjectParameter("in_IDTask", typeof(int));
    
            var in_TypeParameter = in_Type.HasValue ?
                new ObjectParameter("in_Type", in_Type) :
                new ObjectParameter("in_Type", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<fnGetFlatList_Result>("[Entities].[fnGetFlatList](@in_IDTask, @in_Type)", in_IDTaskParameter, in_TypeParameter);
        }
    }
}