
CREATE TABLE public."Clients" (
	"Id" uuid NOT NULL,
	"FirstName" text NOT NULL,
	"LastName" text NOT NULL,
	"Email" text NOT NULL,
	CONSTRAINT "PK_Clients" PRIMARY KEY ("Id")
)
PARTITION BY HASH ("Id");

CREATE TABLE public.clients_s0 PARTITION OF public."Clients"  FOR VALUES WITH (modulus 3, remainder 0);

CREATE TABLE public.clients_s1 PARTITION OF public."Clients"  FOR VALUES WITH (modulus 3, remainder 1);

CREATE TABLE public.clients_s2 PARTITION OF public."Clients"  FOR VALUES WITH (modulus 3, remainder 2);
