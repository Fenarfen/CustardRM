﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM.Models.DTOs;

public class CategoryCreateEdit
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; }
    public string CategoryDescription { get; set; }
}
