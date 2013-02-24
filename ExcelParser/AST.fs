﻿module AST
    open System.Diagnostics
    open Microsoft.Office.Interop.Excel

    type Worksheet = Microsoft.Office.Interop.Excel.Worksheet
    type XLRange = Microsoft.Office.Interop.Excel.Range
    type XLRefStyle = Microsoft.Office.Interop.Excel.XlReferenceStyle

    type Address(R: int, C: int, ws: Worksheet) =
        override self.ToString() =
            "(" + R.ToString() + "," + C.ToString() + ")"
        member self.R1C1 = "R" + R.ToString() + "C" + C.ToString()
        member self.X = C
        member self.Y = R
        member self.worksheet_idx = ws.Index - 1
        member self.Cell() : XLRange = ws.Cells.Range(self.R1C1)
        member self.AddressAsInt32() =
            // convert to zero-based indices
            // the modulus catches overflow; collisions are OK because our equality
            // operator does an exact check
            // underflow should throw an exception
            let ws_idx = (ws.Index - 1) % 32 // allow 5 bits for worksheet index
            let col_idx = (C - 1) % 2048     // allow 11 bits for columns
            let row_idx = R - 1 % 65536      // allow 16 bits for rows
            Debug.Assert(ws_idx >= 0 && col_idx >= 0 && row_idx >= 0)
            row_idx + (col_idx <<< 11) + (ws_idx <<< 16)
        override self.GetHashCode() : int = self.AddressAsInt32()
        override self.Equals(obj: obj) : bool =
            let addr = obj :?> Address
            self.SameAs addr
        member self.SameAs(addr: Address) : bool =
            self.X = addr.X &&
            self.Y = addr.Y &&
            self.worksheet_idx = addr.worksheet_idx
        member self.InsideRange(rng: Range) : bool =
            not (self.X < rng.getXLeft() ||
                 self.Y < rng.getYTop() ||
                 self.X > rng.getXRight() ||
                 self.Y > rng.getYBottom());
        member self.InsideAddr(addr: Address) : bool =
            self.X = addr.X && self.Y = addr.Y

    and Range(topleft: Address, bottomright: Address) =
        let _tl = topleft
        let _br = bottomright  
        override self.ToString() =
            let tlstr = topleft.ToString()
            let brstr = bottomright.ToString()
            tlstr + "," + brstr
        member self.getXLeft() : int = _tl.X
        member self.getXRight() : int = _br.X
        member self.getYTop() : int = _tl.Y
        member self.getYBottom() : int = _br.Y
        member self.InsideRange(rng: Range) : bool =
            not (self.getXLeft() < rng.getXLeft() ||
                 self.getYTop() < rng.getYTop() ||
                 self.getXRight() > rng.getXRight() ||
                 self.getYBottom() > rng.getYBottom());
        // Yup, weird case.  This is because we actually
        // distinguish between addresses and ranges, unlike Excel.
        member self.InsideAddr(addr: Address) : bool =
            not (self.getXLeft() < addr.X ||
                 self.getYTop() < addr.Y ||
                 self.getXRight() > addr.X ||
                 self.getYBottom() > addr.Y);

    type ReferenceRange(wsname: string option, rng: Range) =
        override self.ToString() =
            match wsname with
            | Some(wsn) -> "ReferenceRange(" + wsn.ToString() + ", " + rng.ToString() + ")"
            | None -> "ReferenceRange(None, " + rng.ToString() + ")"
        member self.Range = rng

    type ReferenceAddress(wsname: string option, addr: Address) =
        override self.ToString() =
            match wsname with
            | Some(wsn) -> "ReferenceAddress(" + wsname.ToString() + ", " + addr.ToString() + ")"
            | None -> "ReferenceAddress(None, " + addr.ToString() + ")"
        member self.Address = addr

    type Reference =
    | RangeRef of ReferenceRange
    | AddressRef of ReferenceAddress
        override self.ToString() =
            match self with
            | RangeRef(rr) -> rr.ToString()
            | AddressRef(ar) -> ar.ToString()
        member self.InsideAddr(addr: Address) : bool =
            match self with
            | RangeRef(rr) -> rr.Range.InsideAddr(addr)
            | AddressRef(ar) -> ar.Address.InsideAddr(addr)
        member self.InsideRange(rng: Range) : bool =
            match self with
            | RangeRef(rr) -> rr.Range.InsideRange(rng)
            | AddressRef(ar) -> ar.Address.InsideRange(rng)
        member self.InsideRef(ref: Reference) : bool =
            match ref with
            | RangeRef(rr) -> self.InsideRange(rr.Range)
            | AddressRef(ar) -> self.InsideAddr(ar.Address)