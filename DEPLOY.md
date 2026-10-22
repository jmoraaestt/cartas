# Deploy no Render (Docker) + MySQL no Aiven

1. No Aiven, crie um serviço **MySQL** e anote host, porta e senha do usuário `avnadmin`.
2. No Render, crie um **Web Service** apontando para este repositório, com **Runtime: Docker**
   (usa o `Dockerfile` da raiz).
3. Em **Settings → Health Check Path**, coloque `/health`.
4. Em **Environment**, cadastre as variáveis abaixo.

## Variáveis de ambiente

| Variável | Obrigatória | Exemplo / observação |
|---|---|---|
| `ConnectionStrings__Default` | sim | `Server=HOST;Port=PORTA;Database=defaultdb;User=avnadmin;Password=SENHA;SslMode=Required;` |
| `Seed__EscritoraLogin` | sim | login da escritora, criado no primeiro start |
| `Seed__EscritoraSenha` | sim | senha forte; só vale no primeiro start (nunca sobrescreve) |
| `Seed__LeitoraLogin` | sim | login da leitora |
| `Seed__LeitoraSenha` | sim | senha forte; só vale no primeiro start |
| `ASPNETCORE_ENVIRONMENT` | não | padrão `Production`; **não** use `Development` (o cookie deixa de exigir HTTPS) |
| `PORT` | não | definida pelo próprio Render; sem ela o app escuta na 8080 |

Não há arquivo `.env` em produção: ele fica fora da imagem (`.dockerignore`) e do git.

## O que acontece no start

- As migrations pendentes são aplicadas (`SeedUsuarios.ExecutarAsync`), incluindo a tabela
  `DataProtectionKeys`, e os usuários do seed são criados se ainda não existirem.
- As chaves que cifram o cookie de login ficam no MySQL, então redeploys não deslogam ninguém.
